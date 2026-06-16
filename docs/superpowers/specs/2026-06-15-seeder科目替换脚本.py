# -*- coding: utf-8 -*-
import csv, re, io
SEEDER="src/STOTOP.WebAPI/Data/Seeders/FinanceSeeder.cs"
CHART="docs/superpowers/specs/2026-06-15-新科目表.csv"
TS="2026-06-15 00:00:00.000"

AUX={"网点":"outlet","业务方向":"business_direction","快递品牌":"express_brand","经营单元":"business_unit",
     "客户":"customer","供应商":"supplier","部门":"department","员工":"employee","项目":"project","outlet":"outlet"}
def tr_aux(s):
    if not s or s.strip() in("","_"): return None
    parts=[AUX.get(p.strip(),p.strip()) for p in s.split(",") if p.strip()]
    return ",".join(dict.fromkeys(parts)) if parts else None

# 新科目表(排除1002*/1012*)
COMMON=[]
with open(CHART,encoding="utf-8-sig") as f:
    for r in csv.DictReader(f):
        c=r["F编码"]
        if c.startswith("1002") or c.startswith("1012"): continue
        COMMON.append(dict(code=c,name=r["F名称"],cat=r["F类别"],dir=r["F余额方向"],
                           lvl=int(r["F级次"]),parent=r["F父编码"],leaf=int(r["F是否末级"]),aux=tr_aux(r["F辅助核算"])))

txt=open(SEEDER,encoding="utf-8",errors="ignore").read()
pat=re.compile(r"INSERT INTO \[FIN科目\] \([^)]*\) VALUES \((.*?)\);",re.S)
def splitvals(raw):
    vals=[];cur="";inq=False
    for ch in raw:
        if ch=="'":inq=not inq;cur+=ch
        elif ch==","and not inq:vals.append(cur.strip());cur=""
        else:cur+=ch
    vals.append(cur.strip());return vals
def clean(v):
    v=v.strip()
    if v.startswith("N'")and v.endswith("'"):return v[2:-1]
    if v=="NULL":return None
    return v
keep={"1":[], "2":[]}; orgof={}
for m in pat.finditer(txt):
    v=[clean(x) for x in splitvals(m.group(1))]
    if len(v)<14: continue
    code,setid,org=v[1],v[12],v[13]
    if setid in keep and (str(code).startswith("1002") or str(code).startswith("1012")):
        keep[setid].append(dict(code=code,name=v[2],cat=v[3],dir=v[4],lvl=int(v[5]),
                                parent=(code[:-2] if len(code)>4 else ""),leaf=int(v[7]),aux=v[8]))
    if setid in("1","2") and setid not in orgof: orgof[setid]=org

FID0={"1":600000,"2":700000}
def assemble(setid):
    rows=sorted(keep[setid]+COMMON,key=lambda r:(len(r["code"]),r["code"]))
    code2fid={};fid=FID0[setid]
    for r in rows: fid+=1;code2fid[r["code"]]=fid;r["_fid"]=fid
    for r in rows: r["_pid"]=code2fid.get(r["parent"],0) if r["parent"] else 0
    return rows,orgof.get(setid,"0")

def sv(x):
    if x is None:return "NULL"
    if isinstance(x,int):return str(x)
    return "N'"+str(x).replace("'","''")+"'"

cols="[FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]"
body=io.StringIO()
total=0
for setid in("1","2"):
    rows,org=assemble(setid)
    body.write(f"        -- 账套{setid} 组织ID={org} 共{len(rows)}科目(含现有1002/1012={len(keep[setid])})\n")
    for r in rows:
        total+=1
        vals=f"({r['_fid']}, {sv(r['code'])}, {sv(r['name'])}, {sv(r['cat'])}, {sv(r['dir'])}, {r['lvl']}, {r['_pid']}, {r['leaf']}, {sv(r['aux'])}, NULL, NULL, 1, {int(setid)}, {int(org)}, N'{TS}', N'{TS}', 0, 0)"
        body.write(f"        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = {r['_fid']})\n        INSERT INTO [FIN科目] ({cols}) VALUES {vals};\n")
newblock=('        // FIN科目（2026-06-15 重构：太仓美申全新科目表，两账套各保留1002/1012）\n'
          '        ExecSql(ctx, @"\n'
          '        SET IDENTITY_INSERT [FIN科目] ON;\n'
          + body.getvalue() +
          '        SET IDENTITY_INSERT [FIN科目] OFF;\n        ");\n')

# 定位并替换旧 FIN科目 ExecSql 块
blkpat=re.compile(r'        // FIN科目\n        ExecSql\(ctx, @"\n        SET IDENTITY_INSERT \[FIN科目\] ON;.*?SET IDENTITY_INSERT \[FIN科目\] OFF;\s*"\);\n', re.S)
new_txt, n = blkpat.subn(newblock, txt)
if n!=1:
    print("ERR 未唯一匹配到FIN科目块, n=",n);
else:
    open(SEEDER,"w",encoding="utf-8").write(new_txt)
    print("OK 替换FIN科目块; 新科目总条数=",total)
