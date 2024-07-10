using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Cloud.Functions.Framework;
using Inserat_Function_GoogleCloud;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Google.Apis.Requests.BatchRequest;

namespace WFP1GoogleFunction
{
    public class Function : IHttpFunction
    {
        private const string dbname = "Inserat";
        private const string clientId = "CID";
        private const string clientSecret = "CSECRET";

        public async Task HandleAsync(HttpContext context)
        {

            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
            context.Response.Headers.Append("Access-Control-Max-Age", "3600");
            context.Response.ContentType = "application/json";
            if(context.Request.Method.ToUpper() == "OPTIONS")
            {
                return;
            }
            var request = context.Request;

            if (!request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync($"User is not authenticated.");
                return;
            }

            string idToken = authHeader.ToString().Replace("Bearer ", "");

            try
            {
                var payload = await VerifyGoogleToken(idToken);

                if (payload != null)
                {
                    var req = context.Request;
                    context.Response.ContentType = "application/json";
                    var resp = createApiRespAsync<Inserat>(context,payload.Email);
                    context.Response.StatusCode = resp.Result.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(resp.Result));
                    
                }
                else
                {
                    await ReturnAllHeaders(context, "Invalid ID token.");
                }
            }
            catch (Exception ex)
            {
                await ReturnAllHeaders(context, $"Error verifying ID token: {ex.Message}");
            }
        }

        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { clientId }
            };

            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }

        private async Task ReturnAllHeaders(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var headers = context.Request.Headers;
            var headersString = new StringBuilder();
            headersString.AppendLine(message);
            headersString.AppendLine("Request Headers:");

            foreach (var header in headers)
            {
                headersString.AppendLine($"{header.Key}: {header.Value}");
            }

            await context.Response.WriteAsync(headersString.ToString());
        }

        public async Task<ApiResponse<ICollection<T>>> createApiRespAsync<T>(HttpContext context,string email) where T : Inserat
        {
            var req = context.Request;
            try
            {
                DBAccess dbhandler = new();
                IMongoDatabase database = dbhandler.CreateConnToDb(dbname);

                IMongoCollection<Inserat> inseratcollection = database.GetCollection<Inserat>("Inserat");
                IMongoCollection<Kategorie> kategoriecollection = database.GetCollection<Kategorie>("Kategorie");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<T>(requestBody);

                if (req.Method.ToUpper() == "GET")
                {
                    string id = null;
                    if (req.Query.ContainsKey("getbyinseratID"))
                    {
                        id = req.Query["getbyinseratID"][0];
                        return createApiResp<T>(dbhandler.getByID<T>(id));
                    }
                    else if (req.Query.ContainsKey("getallbykategorieID"))
                    {
                        id = req.Query["getallbykategorieID"][0];
                        return createApiResp<T>(dbhandler.GetAllByKategorieID<T>(ObjectId.Parse(id)));
                    }
                    else if (req.Query.ContainsKey("getallbyuserID"))
                    {
                        id = req.Query["getallbyuserID"][0];
                        return createApiResp<T>(dbhandler.GetAllByUserID<T>(id));
                    }
                    else if (req.Query.ContainsKey("getallkategorien"))
                    {
                        return createApiResp<T>(dbhandler.GetAll<T>());
                    }

                    return createApiResp<T>(dbhandler.GetAll<T>());
                }
                else if (req.Method.ToUpper() == "POST")
                {
                    var coll = dbhandler.GetDynamicCollection<T>();
                    return await handlePostAsync<T>(coll, data, email);
                }

                return new ApiResponse<ICollection<T>>()
                {
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                string h = "";
                foreach (var item in req.Headers)
                {
                    h += item.Key + " : " + item.Value + "\n";
                }

                return new ApiResponse<ICollection<T>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during the operation. Please try again later. \n " + h,
                    StatusCode = 500
                };
            }
        }

        public static async Task<ApiResponse<ICollection<T>>> handlePostAsync<T>(IMongoCollection<T> coll, T toinsert,string email) where T : DBClass
        {
            try
            {
                if (toinsert is Inserat)
                {
                    (toinsert as Inserat).Datum = DateTime.Now;
                    (toinsert as Inserat).Email = email;
                }

                await coll.InsertOneAsync(toinsert);

                return new ApiResponse<ICollection<T>>
                {
                    IsSuccess = true,
                    Data = new List<T> { toinsert },
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ICollection<T>>
                {
                    IsSuccess = false,
                    Data = default(ICollection<T>),
                    ErrorMessage = "Error while insertion into MongoDB",
                    ExceptionMessage = ex.Message,
                    StatusCode = 500
                };
            }
        }

        public static ApiResponse<ICollection<T>> createApiResp<T>(T data)
        {
            return new ApiResponse<ICollection<T>>
            {
                IsSuccess = true,
                Data = new List<T> { data },
            };
        }

        public static ApiResponse<ICollection<T>> createApiResp<T>(ICollection<T> dataList)
        {
            return new ApiResponse<ICollection<T>>
            {
                IsSuccess = true,
                Data = dataList
            };
        }

        public class ApiResponse<T>
        {
            public bool IsSuccess { get; set; }
            public T Data { get; set; }
            public string ErrorMessage { get; set; }
            public string ExceptionMessage { get; set; }
            public int StatusCode { get; set; } = 200;
        }
    }
}
