﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Resonance.Common;
using Subsonic.Common.Classes;
using System.Text;
using System.Xml.Linq;

namespace Resonance.SubsonicCompat
{
    public abstract class SubsonicFilter
    {
        protected SubsonicFilter()
        {
        }

        public static IActionResult ConvertToResultFormat(Response response, SubsonicQueryParameters queryParameters)
        {
            if (response == null || queryParameters == null)
            {
                return null;
            }

            var xmlString = response.SerializeToXml();

            switch (queryParameters.Format)
            {
                case SubsonicReturnFormat.Json:
                    {
                        return CreateContentResult(GetJsonResponse(xmlString), "application/json", Encoding.UTF8);
                    }

                case SubsonicReturnFormat.Jsonp:
                    {
                        return CreateContentResult($"{queryParameters.Callback}({GetJsonResponse(xmlString)});", "text/javascript", Encoding.UTF8);
                    }

                case SubsonicReturnFormat.Xml:
                    {
                        return CreateContentResult(xmlString, "text/xml", Encoding.UTF8);
                    }

                default:
                    {
                        return null;
                    }
            }
        }

        public static ContentResult CreateContentResult(string content, string contentType, Encoding contentEncoding)
        {
            var contentResult = new ContentResult { Content = content };
            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);

            mediaTypeHeader.Encoding = contentEncoding ?? mediaTypeHeader.Encoding;
            contentResult.ContentType = mediaTypeHeader.ToString();

            return contentResult;
        }

        private static string GetJsonResponse(string xmlString)
        {
            var xElement = XElement.Parse(xmlString);

            return xElement.SerializeXObject(Formatting.None, false, false, SubsonicControllerExtensions.GetValueForPropertyName);
        }
    }
}