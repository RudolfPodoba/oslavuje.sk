namespace oslavuje.sk.Utils;

public class EmailUtils
{   public static string ApplyTemplateParameters(string templateText, List<(string ParamName, string ParamValue)> parameters)
    {
        foreach (var (paramName, paramValue) in parameters)
        {
            templateText = templateText.Replace("{" + paramName + "}", paramValue);
        }
        return templateText;
    }
}