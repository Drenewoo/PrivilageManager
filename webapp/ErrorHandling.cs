using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class ErrorHandling
{
    private readonly RequestDelegate _next;

    public ErrorHandling(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Logowanie wyjątku do konsoli
            Console.WriteLine("Wystąpił błąd: " + ex.Message);
            Console.WriteLine(ex.StackTrace);

            // Przekierowanie na /Home/Index
            context.Response.Redirect("/");
        }
    }
}