using System;
public static class ModuleInit
{
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        Console.WriteLine("[MyLibrary] 初期化しました");
    }
}
