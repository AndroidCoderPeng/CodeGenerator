namespace CodeGenerator.Utils
{
    public class RuntimeCache
    {
        //代码量按前、后各连续30页，共60页，（不足60页全部提交）第60页为模块结束页，每页不少于50行（结束页除外）
        public const int EffectiveCodeCount = 60 * 55;
    }
}