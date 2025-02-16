using System;

namespace LicenseManager_12noon.Client;

public static class MyNow
{
	public static Func<DateTime> UtcNow = () => DateTime.UtcNow;

	public static Func<DateTime> Now = () => UtcNow().ToLocalTime();
}
