namespace ClientSide
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Services.AddControllersWithViews();

			builder.Services.AddHttpClient();
			builder.Services.AddHttpContextAccessor();
			builder.Services.AddDistributedMemoryCache();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
                app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();

            app.UseHttpMethodOverride();

            app.UseStaticFiles();

			app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Account}/{action=Login}");

			app.Run();
		}
	}
}
