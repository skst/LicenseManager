using System;

namespace Shared;

public static class MyNow
{
	public static Func<DateTime> UtcNow = () => DateTime.UtcNow;

	public static Func<DateTime> Now = () => UtcNow().ToLocalTime();
}
