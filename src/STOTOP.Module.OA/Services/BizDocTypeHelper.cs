namespace STOTOP.Module.OA.Services;

public static class BizDocTypeHelper
{
    public static string GetDisplayName(string bizDocType) => bizDocType switch
    {
        "expense_request" => "费用请款",
        "expense_reimburse" => "费用报销",
        "external_payment" => "对外付款",
        "petty_cash_apply" => "备用金申请",
        "petty_cash_reimburse" => "备用金报销",
        "petty_cash_return" => "备用金还款",
        "petty_cash_writeoff" => "备用金冲销",
        "salary_advance" => "预支工资",
        "loan_apply" => "借款申请",
        "commission_apply" => "返佣申请",
        _ => bizDocType
    };

    public static string GetStatusText(int status) => status switch
    {
        0 => "进行中",
        1 => "已通过",
        2 => "已拒绝",
        3 => "已撤回",
        4 => "已作废",
        _ => "未知"
    };

    public static string GetActionText(int taskStatus) => taskStatus switch
    {
        1 => "同意",
        2 => "拒绝",
        3 => "退回",
        4 => "转办",
        _ => "未知"
    };
}
