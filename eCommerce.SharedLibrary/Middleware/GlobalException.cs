using eCommerce.SharedLibrary.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace eCommerce.SharedLibrary.Middleware
{
    public class GlobalException(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context) 
        {
            // default variables
            string message = "Sorry, internal server error occurred. Try again.";
            int statusCode = (int)HttpStatusCode.InternalServerError;
            string title = "Error";

            try 
            {
                await next(context);

                // check if the response is too many requests

                if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    title = "Warning";
                    message = "Too many request made.";
                    statusCode = (int)StatusCodes.Status429TooManyRequests;
                    await ModifyHeader(context, title, message, statusCode);
                }

                // check if the response Unauthoried
                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    title = "Alert";
                    message = "You are not authorie to access.";
                    statusCode = (int)StatusCodes.Status401Unauthorized;
                    await ModifyHeader(context, title, message, statusCode);
                }

                // if response is forbidden
                if (context.Response.StatusCode == StatusCodes.Status403Forbidden) 
                {
                    title = "Out of acess";
                    message = "You are not allowed to access.";
                    statusCode = (int)StatusCodes.Status403Forbidden;
                    await ModifyHeader(context, title, message, statusCode);
                }
            } catch (Exception ex) 
            {
                // Log the exception
                LogExceptions.LogException(ex);

                // check exception timout
                if(ex is TaskCanceledException || ex is TimeoutException)
                {
                    title = "Out of time.";
                    message = "Time out... Try again.";
                    statusCode = StatusCodes.Status408RequestTimeout;
                }
                // default exception
                await ModifyHeader(context, title, message, statusCode);
            }
        }

        private static async Task ModifyHeader(HttpContext context, string title, string message, int statusCode)
        {
           // display message
           context.Response.ContentType = "application/json";
           await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails() 
           {
               Detail = message,
               Status = statusCode,
               Title = title
           }), CancellationToken.None);
            return;
        }
    }
}
