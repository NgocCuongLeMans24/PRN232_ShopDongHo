namespace ClientSide.Utils
{
	static public class MyTool
	{
		public static string GetUrl()
		{
			return new ConfigurationBuilder().AddJsonFile("appsettings.json")
				.Build()
				.GetValue<string>("GivenAPIBaseUrl");
		}
	}
}
