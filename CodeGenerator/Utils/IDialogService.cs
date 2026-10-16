namespace CodeGenerator.Utils;

public interface IDialogService
{
    /// <summary>
    /// 显示类型为 T 的对话框，可传入可选参数，并返回表示对话框结果的可空布尔值。
    /// </summary>
    /// <param name="parameter">要传递给对话框的可选参数。</param>
    /// <typeparam name="T">与视图对应的视图模型类型。</typeparam>
    void ShowDialog<T>(object parameter = null) where T : class, new();
}