namespace Log2ui.Dependencies;

public class ProgramRegister : ISelfRegistering
{
    public static void RegisterServices(Registry registry)
    {
        Program.Register(registry);
    }
}
