using GoHireNow.Models.HomeModels;
using GoHireNow.Service.Interfaces;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace GoHireNow.Service.HomeServices
{
    public class HomeService : IHomeService
    {
        public bool SubmitInquiry(ContactUsResponse model)
        {
            String fdDomain = "eva"; // your freshdesk domain
            String apiKey = "gTHdd42zyuxDlxXZvX";
            string apiPath = "/api/v2/tickets"; // API path
            long productId = 43000006921;
            // long productId = 43000004932;
            string json = "{\"status\": 2, \"priority\": 1, \"email\":\"" + model.Email + "\",\"subject\":\"" + model.Title + "\",\"description\":\"" + model.Comment + "\",\"product_id\":" + productId + "}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://" + fdDomain + ".freshdesk.com" + apiPath);
            request.ContentType = "application/json";
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;
            string authInfo = apiKey + ":X"; // It could be your username:password also.
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            request.Headers["Authorization"] = "Basic " + authInfo;


            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string Response = reader.ReadToEnd();
                Console.WriteLine("Status Code: {1} {0}", ((HttpWebResponse)response).StatusCode, (int)((HttpWebResponse)response).StatusCode);
                Console.WriteLine("Location: {0}", response.Headers["Location"]);
                Console.Out.WriteLine(Response);
            }
            catch (WebException ex)
            {
                Console.WriteLine("API Error: Your request is not successful. If you are not able to debug this error properly, mail us at support@freshdesk.com with the follwing X-Request-Id");
                Console.WriteLine("X-Request-Id: {0}", ex.Response.Headers["X-Request-Id"]);
                Console.WriteLine("Error Status Code : {1} {0}", ((HttpWebResponse)ex.Response).StatusCode, (int)((HttpWebResponse)ex.Response).StatusCode);
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    Console.Write("Error Response: ");
                    Console.WriteLine(reader.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(ex.Message);
            }
            return true;
        }

    }
}
