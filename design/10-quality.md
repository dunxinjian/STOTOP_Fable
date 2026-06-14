# Quality 质量中心模块设计文档

## 1. 模块职责与边界

### 1.1 核心职责

- **异常检测**：基于规则引擎自动扫描业务数据，发现异常并创建异常工单
- **规则管理**：配置异常检测规则，支持条件表达式与多条件AND/OR组合
- **异常派发与处理**：异常工单按派发方式（OA流程/工作任务/消息预警）分发处理
- **复盘改进**：异常处理后进行根因分析、改进计划跟踪
- **知识沉淀**：将异常处理经验转化为知识库文章
- **质量绩效**：按维度评估团队/个人的异常处理质量

### 1.2 不负责的内容（明确边界）

| 边界外内容 | 归属模块 |
|---|---|
| 审批流程管理 | OA |
| 任务分配与项目管理 | Task |
| 用户权限、角色、菜单管理 | System |
| 原始数据导入与清洗 | DataCenter |

### 1.3 与其他模块的依赖关系

```mermaid
graph LR
    Quality -->|异常派发为任务| Task
    Quality -->|异常审批流程| OA
    Quality -->|用户权限| System
    Express -->|业务数据异常| Quality
    DataCenter -->|数据质量异常| Quality
```

- **Task**：异常可派发为Task任务（FDispatchMethod=1），关联跟踪处理进度
- **OA**：异常可通过OA审批流程处理（FDispatchMethod=0）
- **System**：用户权限与组织架构查询
- **Express/DataCenter**：作为被检测的业务数据来源

### 1.4 目录结构

```
src/Modules/Quality/
├── Configurations/      # EF Core实体配置（9个）
├── Controllers/         # API控制器（7个）
├── Dtos/                # 数据传输对象（8个）
├── Entities/            # 领域实体（9个）
├── EventHandlers/       # 领域事件处理器
├── Events/              # 领域事件定义
└── Services/            # 业务服务（9个子包）
    ├── Alert/           # 预警服务
    ├── Dashboard/       # 仪表板服务
    ├── Detection/       # 检测服务
    ├── Dispatch/        # 派发服务
    ├── Exception/       # 异常管理服务
    ├── Knowledge/       # 知识库服务
    ├── Performance/     # 绩效评估服务
    ├── Review/          # 复盘服务
    └── Rule/            # 规则管理服务
```

---

## 2. 数据库表设计

### 2.1 异常管理核心（2张表）

#### QlException — 异常工单表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FExceptionNo | NVARCHAR(50) | 异常编号（唯一，自动生成） |
| FTitle | NVARCHAR(200) | 异常标题 |
| FDescription | NVARCHAR(MAX) | 异常描述 |
| FType | INT | 异常类型：0=数据异常, 1=流程超时, 2=规则违规 |
| FStatus | INT | 状态：0=待处理, 1=处理中, 2=已超时, 3=已关闭 |
| FPriority | INT | 优先级：0=低, 1=中, 2=高, 3=紧急 |
| FRuleId | BIGINT FK | 触发规则ID（可空，手动创建时为空） |
| FBusinessLine | NVARCHAR(50) | 业务线：express_outbound/express_inbound/order_processing/crm/finance |
| FSourceTable | NVARCHAR(100) | 来源数据表名 |
| FSourceRecordId | NVARCHAR(50) | 来源记录ID |
| FDispatchMethod | INT | 派发方式：0=OA流程, 1=工作任务, 2=消息预警 |
| FDispatchTargetId | BIGINT | 派发目标ID（OA实例ID/Task任务ID） |
| FAssigneeId | BIGINT | 处理人ID |
| FAssigneeName | NVARCHAR(50) | 处理人姓名 |
| FDueTime | DATETIME2 | 处理截止时间 |
| FClosedTime | DATETIME2 | 关闭时间 |
| FClosedBy | BIGINT | 关闭人ID |
| FCloseReason | NVARCHAR(500) | 关闭原因 |
| FOrgId | BIGINT | 组织ID |
| FCreatorId | BIGINT | 创建人ID |
| FCreatedTime | DATETIME2 | 创建时间 |
| FModifiedTime | DATETIME2 | 修改时间 |
| FIsDeleted | BIT | 软删除 |

#### QlExceptionLog — 异常操作日志表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FExceptionId | BIGINT FK | 关联异常工单 |
| FActionType | NVARCHAR(50) | 操作类型：create/assign/status_change/comment/close |
| FDescription | NVARCHAR(500) | 操作描述 |
| FOperatorId | BIGINT | 操作人ID |
| FOperatorName | NVARCHAR(50) | 操作人姓名 |
| FChanges | NVARCHAR(MAX) | 变更详情JSON |
| FCreatedTime | DATETIME2 | 操作时间 |

### 2.2 规则检测（2张表）

#### QlRule — 检测规则表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FRuleName | NVARCHAR(200) | 规则名称 |
| FDescription | NVARCHAR(500) | 规则描述 |
| FBusinessLine | NVARCHAR(50) | 业务线 |
| FConditionExpression | NVARCHAR(MAX) | 条件表达式（如"weight > 100 AND status = 'pending'"） |
| FTargetTable | NVARCHAR(100) | 检测目标表名 |
| FDispatchMethod | INT | 默认派发方式：0=OA流程, 1=工作任务, 2=消息预警 |
| FDefaultAssigneeId | BIGINT | 默认处理人ID |
| FDefaultPriority | INT | 默认优先级 |
| FTimeoutHours | INT | 超时小时数 |
| FCronExpression | NVARCHAR(100) | 检测频率Cron表达式 |
| FIsEnabled | BIT | 是否启用 |
| FSortOrder | INT | 排序号 |
| FOrgId | BIGINT | 组织ID |
| FCreatorId | BIGINT | 创建人ID |
| FCreatedTime | DATETIME2 | 创建时间 |
| FModifiedTime | DATETIME2 | 修改时间 |

#### QlRuleCondition — 规则条件明细表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FRuleId | BIGINT FK | 关联规则 |
| FFieldName | NVARCHAR(100) | 字段名 |
| FOperator | NVARCHAR(20) | 操作符：=, !=, >, <, >=, <=, LIKE, IN, IS NULL |
| FValue | NVARCHAR(500) | 阈值/比较值 |
| FLogicRelation | NVARCHAR(5) | 与下一条件逻辑关系：AND/OR |
| FSortOrder | INT | 排序号（决定条件计算顺序） |

### 2.3 预警配置（1张表）

#### QlAlertConfig — 预警配置表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FRuleId | BIGINT FK | 关联规则（可空，全局预警时为空） |
| FAlertName | NVARCHAR(200) | 预警名称 |
| FThresholdType | NVARCHAR(50) | 阈值类型：count=数量, rate=比率, time=时间 |
| FThresholdValue | DECIMAL(18,2) | 阈值 |
| FNotifyMethod | INT | 通知方式：0=站内消息, 1=钉钉, 2=邮件, 3=多渠道 |
| FNotifyTargetIds | NVARCHAR(500) | 通知对象ID列表 |
| FNotifyRoleIds | NVARCHAR(500) | 通知角色ID列表 |
| FIsEnabled | BIT | 是否启用 |
| FOrgId | BIGINT | 组织ID |
| FCreatedTime | DATETIME2 | 创建时间 |

### 2.4 复盘改进（2张表）

#### QlReview — 复盘记录表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FExceptionId | BIGINT FK | 关联异常工单 |
| FTitle | NVARCHAR(200) | 复盘标题 |
| FRootCause | NVARCHAR(MAX) | 根因分析 |
| FImpactAnalysis | NVARCHAR(MAX) | 影响分析 |
| FConclusion | NVARCHAR(MAX) | 结论与总结 |
| FReviewerId | BIGINT | 复盘人ID |
| FReviewDate | DATE | 复盘日期 |
| FOrgId | BIGINT | 组织ID |
| FCreatedTime | DATETIME2 | 创建时间 |
| FIsDeleted | BIT | 软删除 |

#### QlReviewImprovement — 改进项表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FReviewId | BIGINT FK | 关联复盘记录 |
| FImprovementTitle | NVARCHAR(200) | 改进项标题 |
| FDescription | NVARCHAR(1000) | 改进描述 |
| FResponsibleId | BIGINT | 责任人ID |
| FTargetDate | DATE | 目标完成日期 |
| FStatus | INT | 状态：0=待执行, 1=执行中, 2=已完成, 3=已取消 |
| FEffectDescription | NVARCHAR(500) | 效果描述 |
| FCompletedDate | DATE | 实际完成日期 |
| FCreatedTime | DATETIME2 | 创建时间 |

### 2.5 知识积累（1张表）

#### QlKnowledge — 质量知识库表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FTitle | NVARCHAR(200) | 知识标题 |
| FCategory | NVARCHAR(50) | 分类：best_practice/troubleshooting/standard/faq |
| FContent | NVARCHAR(MAX) | 知识内容（富文本） |
| FExceptionId | BIGINT FK | 关联异常ID（可空） |
| FReviewId | BIGINT FK | 关联复盘ID（可空） |
| FTags | NVARCHAR(500) | 标签（逗号分隔） |
| FAuthorId | BIGINT | 作者ID |
| FViewCount | INT | 浏览次数 |
| FOrgId | BIGINT | 组织ID |
| FCreatedTime | DATETIME2 | 创建时间 |
| FModifiedTime | DATETIME2 | 修改时间 |
| FIsDeleted | BIT | 软删除 |

### 2.6 绩效评估（1张表）

#### QlPerformance — 质量绩效表

| 字段名 | 类型 | 说明 |
|---|---|---|
| FID | BIGINT PK | 主键 |
| FUserId | BIGINT | 被评估人ID |
| FDepartmentId | BIGINT | 部门ID |
| FPeriodStart | DATE | 评估周期开始 |
| FPeriodEnd | DATE | 评估周期结束 |
| FExceptionCount | INT | 总异常数 |
| FResolvedCount | INT | 已解决数 |
| FOverdueCount | INT | 超期数 |
| FAvgResolveHours | DECIMAL(8,2) | 平均处理时长（小时） |
| FScore | DECIMAL(5,2) | 综合得分 |
| FOrgId | BIGINT | 组织ID |
| FCreatedTime | DATETIME2 | 创建时间 |

---

## 3. 规则引擎

### 3.1 条件表达式

规则支持通过 `FConditionExpression` 定义检测条件：

```
// 单条件
weight > 100

// 多条件组合
weight > 100 AND status = 'pending'

// 复杂条件
(amount > 50000 OR priority = '紧急') AND created_days > 3
```

### 3.2 条件组合（QlRuleCondition）

通过 `QlRuleCondition` 表以结构化方式定义多条件组合：

| 序号 | 字段名 | 操作符 | 值 | 逻辑关系 |
|---|---|---|---|---|
| 1 | weight | > | 100 | AND |
| 2 | status | = | pending | AND |
| 3 | created_days | > | 3 | — |

### 3.3 内置业务线

| 业务线编码 | 说明 | 检测目标 |
|---|---|---|
| express_outbound | 快递出港 | 运单数据、计费结果 |
| express_inbound | 快递进港 | 进港运单、签收数据 |
| order_processing | 订单处理 | 订单状态、处理时效 |
| crm | 客户关系 | 客户跟进、商机转化 |
| finance | 财务管理 | 账单异常、对账差异 |

### 3.4 自动检测流程

```
DetectionService（定时/手动触发）
  → 读取启用的 QlRule 列表
  → 遍历规则，按 FConditionExpression/QlRuleCondition 构建查询
  → 查询 FTargetTable 匹配数据
  → 匹配结果 → 创建 QlException
  → 按 FDispatchMethod 派发处理
    → 0: 创建OA审批流程实例
    → 1: 创建Task模块任务
    → 2: 发送预警消息通知
```

### 3.5 质量评分公式

```
综合得分 = 处理完成率 × 50 + 处理及时率 × 20 + 处理质量评分 × 30

其中：
  处理完成率 = (总异常数 - 超期数) / 总异常数 × 100
  处理及时率 = 按时关闭数 / 已关闭总数 × 100
  处理质量评分 = 复盘改进有效率 × 100
```

---

## 4. API接口清单（7个Controller）

### 4.1 异常管理

- `GET /api/quality/exceptions` — 获取异常工单列表（支持筛选/分页）
- `GET /api/quality/exceptions/{id}` — 获取异常工单详情
- `POST /api/quality/exceptions` — 手动创建异常工单
- `PUT /api/quality/exceptions/{id}` — 更新异常工单
- `PUT /api/quality/exceptions/{id}/assign` — 分配处理人
- `POST /api/quality/exceptions/{id}/close` — 关闭异常工单
- `GET /api/quality/exceptions/{id}/logs` — 获取异常操作日志

### 4.2 规则管理

- `GET /api/quality/rules` — 获取规则列表
- `GET /api/quality/rules/{id}` — 获取规则详情（含条件明细）
- `POST /api/quality/rules` — 创建规则
- `PUT /api/quality/rules/{id}` — 更新规则
- `DELETE /api/quality/rules/{id}` — 删除规则
- `POST /api/quality/rules/{id}/enable` — 启用规则
- `POST /api/quality/rules/{id}/disable` — 禁用规则
- `POST /api/quality/rules/{id}/test` — 测试规则（试运行不创建异常）

### 4.3 复盘管理

- `GET /api/quality/reviews` — 获取复盘列表
- `GET /api/quality/reviews/{id}` — 获取复盘详情
- `POST /api/quality/reviews` — 创建复盘记录
- `PUT /api/quality/reviews/{id}` — 更新复盘记录
- `DELETE /api/quality/reviews/{id}` — 删除复盘记录
- `POST /api/quality/reviews/{reviewId}/improvements` — 添加改进项
- `PUT /api/quality/reviews/improvements/{id}` — 更新改进项

### 4.4 知识库

- `GET /api/quality/knowledge` — 获取知识列表（支持分类/搜索）
- `GET /api/quality/knowledge/{id}` — 获取知识详情
- `POST /api/quality/knowledge` — 创建知识文章
- `PUT /api/quality/knowledge/{id}` — 更新知识文章
- `DELETE /api/quality/knowledge/{id}` — 删除知识文章

### 4.5 绩效评估

- `GET /api/quality/performance/records` — 获取绩效记录列表
- `GET /api/quality/performance/records/{userId}` — 获取个人质量绩效
- `POST /api/quality/performance/calculate` — 计算指定周期绩效

### 4.6 仪表板

- `GET /api/quality/dashboard/summary` — 获取质量概览（异常总数/待处理/已超时/已关闭）
- `GET /api/quality/dashboard/trend` — 获取异常趋势数据（按日/周/月）
- `GET /api/quality/dashboard/top-issues` — 获取Top异常问题排行
- `GET /api/quality/dashboard/by-business-line` — 按业务线统计
- `GET /api/quality/dashboard/by-type` — 按异常类型统计

### 4.7 预警配置

- `GET /api/quality/alerts/config` — 获取预警配置列表
- `POST /api/quality/alerts/config` — 创建预警配置
- `PUT /api/quality/alerts/config/{id}` — 更新预警配置
- `DELETE /api/quality/alerts/config/{id}` — 删除预警配置

---

## 5. 业务流程图

### 5.1 异常处理全流程

```mermaid
flowchart TD
    A[DetectionService定时检测] --> B{规则匹配?}
    B -->|是| C[创建QlException异常工单]
    B -->|否| A
    C --> D{派发方式}
    D -->|OA流程| E[创建OA审批实例]
    D -->|工作任务| F[创建Task任务]
    D -->|消息预警| G[发送通知消息]
    E --> H[处理人处理]
    F --> H
    G --> H
    H --> I{处理结果}
    I -->|已解决| J[关闭异常工单]
    I -->|超时未处理| K[升级处理/通知上级]
    K --> H
    J --> L[创建复盘记录]
    L --> M[根因分析]
    M --> N[制定改进计划]
    N --> O[跟踪改进执行]
    O --> P{改进有效?}
    P -->|是| Q[沉淀为知识库]
    P -->|否| N
    Q --> R[质量绩效评估]
```

### 5.2 规则与预警流程

```mermaid
flowchart TD
    A[管理员配置规则] --> B[设定条件表达式]
    B --> C[配置派发方式]
    C --> D[配置预警阈值]
    D --> E[启用规则]
    E --> F[DetectionService按Cron定时执行]
    F --> G[读取启用规则列表]
    G --> H[构建SQL查询条件]
    H --> I[查询目标表]
    I --> J{匹配结果?}
    J -->|有匹配| K[创建异常工单]
    J -->|无匹配| F
    K --> L{触发预警?}
    L -->|异常数超阈值| M[QlAlertConfig触发]
    M --> N[发送预警通知]
    L -->|未超阈值| O[正常派发处理]
    N --> O
```

### 5.3 异常状态流转

```mermaid
stateDiagram-v2
    [*] --> 待处理: 检测创建/手动创建
    待处理 --> 处理中: 分配处理人
    待处理 --> 已超时: 超过截止时间
    处理中 --> 已关闭: 处理完成关闭
    处理中 --> 已超时: 超过截止时间
    已超时 --> 处理中: 重新分配/升级处理
    已关闭 --> [*]
```

---

## 6. 模块关联总览

| 关联模块 | 关联方式 | 说明 |
|---|---|---|
| Task | QlException.FDispatchTargetId → TmTask | 异常派发为任务，Task模块跟踪处理 |
| OA | QlException.FDispatchTargetId → BpmProcessInstance | 异常通过OA审批流程处理 |
| System | 用户/角色/权限依赖 | 处理人分配、权限控制 |
| Express | 业务数据来源 | 检测出港/进港运单异常 |
| DataCenter | 数据质量来源 | 检测导入数据质量异常 |
| Finance | 财务数据来源 | 检测账单、对账差异异常 |
| CRM | 客户数据来源 | 检测客户跟进、商机转化异常 |
