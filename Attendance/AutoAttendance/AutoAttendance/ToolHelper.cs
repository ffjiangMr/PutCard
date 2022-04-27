namespace AutoAttendance
{
    #region using directive

    using HtmlAgilityPack;
    using OpenCvSharp;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Text.Json;

    #endregion

    /// <summary>
    /// 工具集合类
    /// </summary>
    internal static class ToolHelper
    {
        /// <summary>
        /// 解析传入的Json文件，获取指定的Key/Value 集合
        /// </summary>
        /// <param name="input">Json文件</param>
        /// <param name="keys">指定的Key集合</param>
        /// <returns>指定的Key/Value 集合</returns>
        ///  Exceptions:
        ///  T:System.Text.Json.JsonException:
        ///    json does not represent a valid single JSON value.
        ///    
        ///  T:System.ArgumentException:
        ///    options contains unsupported options.
        ///    
        ///   T:System.InvalidOperationException:
        ///     This value's System.Text.Json.JsonElement.ValueKind is not System.Text.Json.JsonValueKind.Object.        
        ///     
        ///   T:System.Collections.Generic.KeyNotFoundException:
        ///     No property was found with the requested name.
        ///
        ///   T:System.ArgumentNullException:
        ///     propertyName is null.
        ///
        ///   T:System.ObjectDisposedException:
        ///     The parent System.Text.Json.JsonDocument has been disposed.
        ///     
        public static Dictionary<String, String> GetValueByKeyFromJson(ref String input, params String[] keys)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            var document = JsonDocument.Parse(input);
            foreach (var item in keys)
            {
                result[item] = document.RootElement.GetProperty(item).GetRawText().Trim('\"');
            }

            return result;
        }

        /// <summary>
        /// 解析传入验证码图片，返回验证码
        /// </summary>
        /// <param name="codeImagePath">验证码图片路径</param>
        /// <returns>验证码</returns>
        ///  Exceptions: 
        ///  T:System.ArgumentNullException:
        ///    propertyName is null.
        ///    
        ///  T:System.ArgumentException:
        ///    options contains unsupported options.
        ///    
        ///   T:System.InvalidOperationException:
        ///     This value's System.Text.Json.JsonElement.ValueKind is not System.Text.Json.JsonValueKind.Object.        
        ///     
        ///   T:System.Collections.Generic.KeyNotFoundException:
        ///     No property was found with the requested name.
        /// 
        ///   T:System.IndexOutOfRangeException:
        ///     An index is outside its bounds.
        /// 
        public static Int32 CalculateCodeIndex(ref String codeImagePath)
        {
            Int32 result = Int32.MaxValue;
            if (File.Exists(codeImagePath) == true)
            {
                using (var mat = new Mat(codeImagePath, ImreadModes.Grayscale))
                {
                    var bitmap = new Bitmap(mat.Threshold(254, 255, ThresholdTypes.Binary).ToMemoryStream());
                    for (Int32 row = 0; row < bitmap.Height; row++)
                    {
                        for (Int32 column = 0; column < bitmap.Width; column++)
                        {
                            if (bitmap.GetPixel(column, row).R == 255)
                            {                                
                                if (result > column)
                                {
                                    result = column + 1;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 解析传入Html的Input信息，获取表单提交数据
        /// </summary>
        /// <param name="input">Html页面</param>
        /// <param name="imageRandeCode">请求包含验证码时，需要传入</param>
        /// <returns>待提交的表单数据</returns>
        public static Dictionary<String, String> GetFormDataFromHtml(ref String input, String imageRandeCode = "")
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(input);
            var inputNodes = document.DocumentNode.SelectNodes("//input");
            if (inputNodes != null)
            {
                foreach (var node in inputNodes)
                {
                    if (node.Attributes.Contains("name") == true)
                    {
                        var name = node.GetAttributeValue("name", "");
                        var value = node.GetAttributeValue("value", "");
                        var classValue = node.GetAttributeValue("class", "");
                        if (classValue.Contains("imageRandeCode") == true)
                        {
                            result[name] = imageRandeCode;
                        }
                        else if (classValue.Contains("userName") == true)
                        {
                            result[name] = UserEntity.GetInstance().UserName;
                        }
                        else if (classValue.Contains("password") == true)
                        {
                            result[name] = UserEntity.GetInstance().Password;
                        }
                        else if (name.Contains("browser") == true)
                        {
                            result[name] = "Chrome";
                        }
                        else
                        {
                            result[name] = value;
                        }
                    }
                }
            }
            return result;
        }
    }
}
