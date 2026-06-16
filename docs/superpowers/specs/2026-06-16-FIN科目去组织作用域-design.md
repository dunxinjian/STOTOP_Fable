# FIN科目去组织作用域重构 — 设计文档

- 日期：2026-06-16
- 范围：`STOTOP.Module.Finance`（连带 `STOTOP.Core`、`AmoebaPLService`）
- 类型：纯重构（修正实体作用域语义，不改业务语义）

## 1. 背景与问题

`FinAccount`（FIN科目）实现了 `IOrgScoped`。`STOTOPDbContext` 给所有 `IOrgScoped`
实体注册了"按 `FOrgId` 的全局组织查询过滤器"（[STOTOPDbContext.cs:115-146](../../../src/STOTOP.Infrastructure/Data/STOTOPDbContext.cs)）。

但科目本质是**账套级共享**：账套（`FinAccountSet`）本身已按组织授权，科目只按
`FAccountSetId` 限定，且同一账套可跨组织共享。把科目纳入组织过滤器是错误的作用域，
导致：

- 满地写 `.IgnoreQueryFilters()` 绕过组织过滤器（AccountService 12 处、AmoebaPLService 5 处）。
- `FOrgId` 沦为"接口要求 + 审计字段"，不参与任何业务过滤，且**全程无索引**——
  证明它只是过滤器的附庸，不是业务分区键。
- 凡是**没有**加绕过的查询点（VoucherService、ReportService、AccountSetService 等
  十余处），在带组织上下文时会因 `FOrgId` 不匹配而漏查，存在跨组织共享账套下的隐性 bug。

同样问题存在于两个"兄弟实体"：`FinAccountBalance`（科目余额）、`FinAuxiliaryBalance`
（辅助核算余额）——结构一致（`FAccountSetId` + `FOrgId`，`FOrgId` 无索引）。

## 2. 目标与非目标

### 目标
- 让 `FinAccount` / `FinAccountBalance` / `FinAuxiliaryBalance` 不再受组织全局过滤器约束。
- 移除因此变得多余的 `.IgnoreQueryFilters()`，使代码意图清晰、不易被误解。
- 保留 `FOrgId` 字段（审计用），不破坏建库/现有数据。
- `dotnet build` 通过；科目/凭证/余额相关查询无回归。

### 非目标（明确排除）
- 不改科目结构（字段、层级、编码规则）或其它业务逻辑。
- 不动 `FinVoucher` / `FinVoucherEntry` 的组织作用域——凭证**确实**是组织级。
- 不动 `FinAccountPeriod` 等其它 `IOrgScoped` 实体（虽可能是同类候选，但不在本次范围）。
- 不动 `AuxiliaryService` 对 `FinAuxiliaryItem` 的绕过——那是另一回事（全局品牌
  `FOrgId=0` 共享），与本次的 `FinAuxiliaryBalance` 是不同实体，勿混淆。
- 不删 `FOrgId` 字段，不加 EF 迁移（移除接口不改表结构）。

## 3. 已确认的决策

| 决策 | 选择 |
|---|---|
| D1 实体范围 | **三个全改**：`FinAccount` + `FinAccountBalance` + `FinAuxiliaryBalance` |
| D2 实现方式 | **新增 `IAccountSetScoped` 标记接口**（不挂全局过滤器）；`FOrgId` 注释为审计字段 |
| D3 AmoebaPL | **一并清理** AmoebaPLService 的 5 处冗余绕过 |
| D4 FOrgId 写入 | 原依赖自动填充的写入点**接受 `FOrgId=0`**；不新增组织上下文管线；AccountService 现有两处显式赋值保留 |

## 4. 设计

### 4.1 新增标记接口 `IAccountSetScoped`

文件：`src/STOTOP.Core/Models/IAccountSetScoped.cs`（与 `IOrgScoped` 同目录）

```csharp
namespace STOTOP.Core.Models;

/// <summary>
/// 标记"账套级共享"实体：按 FAccountSetId 限定，不随组织隔离。
/// 与 IOrgScoped 互斥——实现本接口的实体【不】被组织全局查询过滤器约束。
/// 账套本身已按组织授权（FinAccountSetAuthorization），账套即访问边界。
/// 实体上的 FOrgId（若有）仅为"创建者组织"审计字段，不参与任何过滤，且不建索引。
/// </summary>
public interface IAccountSetScoped
{
    long FAccountSetId { get; set; }
}
```

- 纯标记（带 `FAccountSetId` 以文档化作用域键，三实体本就有此字段，零成本）。
- **不在 `STOTOPDbContext` 注册任何过滤器**——这是它与 `IOrgScoped` 的本质区别。

### 4.2 三个实体改造

`FinAccount` / `FinAccountBalance` / `FinAuxiliaryBalance`：

- `: BaseEntity, IOrgScoped` → `: BaseEntity, IAccountSetScoped`
- `FOrgId` 注释从 `// 组织ID` 改为 `// 创建者组织（审计字段，不参与过滤）`
- 字段、其余成员不变。

### 4.3 `STOTOPDbContext` 无需改动

- 组织过滤器循环（[:115-122](../../../src/STOTOP.Infrastructure/Data/STOTOPDbContext.cs)）按
  `IOrgScoped` 收集实体；三实体不再实现该接口，自动不被收集——无需改循环。
- `FillOrgIdForNewEntities`（[:171-193](../../../src/STOTOP.Infrastructure/Data/STOTOPDbContext.cs)）
  按 `IOrgScoped` 自动填 `FOrgId`；三实体退出后不再被自动填充（见 4.5）。
- `IAccountSetScoped` 不挂过滤器，故 DbContext **零改动**。这是低风险点。

### 4.4 清理 `IgnoreQueryFilters`（精确清单）

> 以下为 HEAD 行号，编辑后会位移；实施时按"方法名 + 实体"定位。

**`AccountService.cs` — 删 12 处，保留 1 处：**

| 行 | 方法 | 实体 | 处理 |
|---|---|---|---|
| 46 | GetTreeAsync | FinAccount | 删 |
| 105 | LoadAccountAsync | FinAccount | 删 |
| 127 | CreateAsync（查重） | FinAccount | 删 |
| 249 | DeleteAsync（凭证引用检查） | **FinVoucherEntry** | **保留**（凭证是组织级，跨组织查引用合理） |
| 259 | DeleteAsync（子科目检查） | FinAccount | 删 |
| 269 | DeleteAsync（清余额行） | FinAccountBalance | 删 |
| 283 | DeleteAsync（兄弟科目） | FinAccount | 删 |
| 320 | GetInitialBalancesAsync（科目） | FinAccount | 删 |
| 328 | GetInitialBalancesAsync（余额） | FinAccountBalance | 删 |
| 386 | SaveInitialBalancesAsync（科目校验） | FinAccount | 删 |
| 409 | SaveInitialBalancesAsync（余额查） | FinAccountBalance | 删 |
| 448 | GetByAuxTypeAsync | FinAccount | 删 |
| 462 | UpdateAccountAuxiliaryAsync | FinAccount | 删 |

删除时一并清理/修正同行的"绕过组织过滤"注释（避免遗留误导性注释）。
保留的 249 行注释保持不变（说明为何跨组织查凭证）。

**`AmoebaPLService.cs` — 删 5 处（均 FinAccount）：**
行 1870 / 2696 / 2835 / 3208 / 3694，注释同款"FIN科目…绕过 IOrgScoped"。

**合计：删 17 处，保留 1 处。** `FinAuxiliaryBalance` 无任何 `IgnoreQueryFilters`（仅在
ReportService 注入），改造后其读取从"被组织过滤"变为"不过滤"，属隐性跨组织 bug 的修复。

### 4.5 `FOrgId` 写入点

移除 `IOrgScoped` 后，`FillOrgIdForNewEntities` 不再填充三实体。处理：

- **保留**现有显式赋值：`AccountService.CreateAsync`（[:188](../../../src/STOTOP.Module.Finance/Services/AccountService.cs)）、
  `SaveInitialBalancesAsync`（[:429](../../../src/STOTOP.Module.Finance/Services/AccountService.cs)）。
- **接受 `FOrgId=0`** 的写入点（原靠自动填充，本次不补组织上下文管线）：
  - `AccountSetService.InitializeAccountSetAsync`（建科目 ≈ :412）
  - `AccountTemplateService.CopyFromTemplateAsync`（建科目 ≈ :331）
  - `AccountSetService.InitializeAccountSetAsync`（建余额 ≈ :534）
  - `AccountPeriodService.OpenNextPeriodAsync`（结转建余额 ≈ :389）
  - 理由：`FOrgId` 已不参与过滤、无索引；这些是账套级/模板/结转行，本非组织私有，
    `0`（未指定）语义上是诚实的，且避免给这些服务新增 `GetCurrentOrgId` 管线（属范围外改动）。

### 4.6 顺手修正：`ReportService.GetAccountBalanceAsync`

[:74](../../../src/STOTOP.Module.Finance/Services/ReportService.cs) 当前 `Query().ToListAsync()`
无 `Where`，靠组织过滤器隐式限定。改造后会加载全部账套科目（结果仍正确——余额已按账套
限定、按主键 `FID` 匹配且 FID 全局唯一，仅多加载行）。**补 `.Where(a => a.FAccountSetId == accountSetId)`**
保持范围与效率。这是小幅安全改进，非回归。

## 5. 行为变化分析（为何安全）

移除组织过滤器后，所有对三实体的查询从"按当前组织过滤"变为"不过滤"。逐类核实：

1. **已按 `FAccountSetId` 限定的读取**（绝大多数）：变为"返回该账套全部行（不分组织）"，
   即跨组织共享账套**应有的正确行为**。账套授权是真边界。
2. **按主键的读取**（`Repository.GetByIdAsync` → `FindAsync`，[Repository.cs:20](../../../src/STOTOP.Infrastructure/Repositories/Repository.cs)）：
   `FindAsync` 会套用组织过滤器。VoucherService 录入凭证按 `AccountId` 取科目时，今天若科目
   `FOrgId` 与当前组织不符就误报"科目不存在"；移除后**修复**（正是任务要解决的症状）。
3. **`AccountSetService` 初始化**：今天"是否已初始化"计数、强制重置清理只看当前组织，
   跨组织会漏判、漏删；移除后**修复**。
4. **`ReportService` / `TrialBalanceService` / `JournalService`**：均按 `FAccountSetId`
   或 `accountIds` 间接限定，行为正确，不受影响。

> 安全前提（沿用现有代码已依赖的假设）：账套访问通过 `FinAccountSetAuthorization` 授权，
> 调用方只在已授权账套内传入 `FAccountSetId`。本次只是把现有 17 处绕过已假设的前提
> 推广到全部查询点，不引入新假设。

## 6. 验收与验证

- [ ] `dotnet build` 通过。
- [ ] 全仓库不再有针对三实体的 `IgnoreQueryFilters`（grep 复核）；`FinVoucherEntry`
      那处保留。
- [ ] 科目查询正常：建/查/树/查重/按辅助核算筛选。
- [ ] 跨组织共用同一账套：在 A 组织建的科目，B 组织（已授权该账套）可见。
- [ ] 按主键 `FindAsync` 取科目正常，不再误报"科目不存在"。
- [ ] 凭证录入/更新/红冲/校验取科目正常。
- [ ] 期初余额录入/汇总、期间结转、试算平衡、阿米巴报表无回归。
- [ ] 账套初始化（含强制重置）正常。

## 7. 风险与缓解

| 风险 | 等级 | 缓解 |
|---|---|---|
| 漏改某查询点导致行为不一致 | 中 | grep 复核 + 验收逐项；DbContext 零改动收敛风险面 |
| 误删合法的 `FinVoucherEntry` 跨组织绕过 | 中 | 清单明确标注保留；按方法名+实体定位 |
| `FOrgId=0` 影响审计可读性 | 低 | 已在 D4 记录；非功能字段，review 可改口径 |
| `FinAuxiliaryBalance` 读取行为变化 | 低 | 仅 ReportService 注入；属跨组织 bug 修复 |

## 8. 涉及文件清单

- 新增：`src/STOTOP.Core/Models/IAccountSetScoped.cs`
- 改实体：`FinAccount.cs`、`FinAccountBalance.cs`、`FinAuxiliaryBalance.cs`
- 清绕过：`AccountService.cs`（12 删 1 留）、`AmoebaPLService.cs`（5 删）
- 小修：`ReportService.cs`（补账套限定）
- 不改：`STOTOPDbContext.cs`、`Repository.cs`、`IOrgScoped.cs`
