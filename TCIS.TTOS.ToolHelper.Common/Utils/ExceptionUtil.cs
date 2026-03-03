namespace TCIS.TTOS.HelperTool.Common.Utils
{
    public static class ExceptionUtil
    {
        public static string GetDetailExceptionMessage(Exception? ex)
        {
            var messages = new List<string>();
            while (ex != null)
            {
                messages.Add(ex.Message);
                ex = ex.InnerException;
            }
            return string.Join(" --> ", messages);
        }
    }
}
