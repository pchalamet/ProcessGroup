using System.Runtime.InteropServices;
namespace BetterStartProcess;

//
// Attempt to start a child process and enroll it in the same process group as the parent.
// Goal is to be able to kill the child process if the parent process is killed.
//
public static class ForkProcess
{
    // Import getpid (to get current PID) from libc
    [DllImport("libc")]
    private static extern int getpid();

    // Import getppid (to get parent PID) from libc
    [DllImport("libc")]
    private static extern int getppid();

    // Import getsid (to get session ID) from libc
    [DllImport("libc")]
    private static extern int getsid(int pid);

    // Import setpgid (to set process group ID) from libc
    [DllImport("libc")]
    private static extern int setpgid(int pid, int pgid);

    // Import getpgid (to get process group ID) from libc
    [DllImport("libc")]
    private static extern int getpgid(int pid);


    // Import posix_spawn (to spawn a new process) from libc
    [DllImport("libc")]
    private static extern int posix_spawn(
        out int pid,
        string path,
        IntPtr fileActions,
        IntPtr attrp,
        string?[] argv,
        string?[] envp);

    // Import posix_spawnattr_init (to initialize spawn attributes) from libc
    [DllImport("libc")]
    private static extern int posix_spawnattr_init(out IntPtr attrp);

    // Import posix_spawnattr_destroy (to destroy spawn attributes) from libc
    [DllImport("libc")]
    private static extern int posix_spawnattr_destroy(IntPtr attrp);

    // Import posix_spawnattr_setpgroup (to set process group in spawn attributes) from libc
    [DllImport("libc")]
    private static extern int posix_spawnattr_setpgroup(IntPtr attrp, int pgroup);

    // Import strerror (to get error message) from libc
    [DllImport("libc")]
    private static extern IntPtr strerror(int errnum);

    // Import execv (to execute a new process) from libc
    [DllImport("libc")]
    private static extern int execv(string path, string[] argv);

    // Import execvp (to execute a new process - search path) from libc
    [DllImport("libc")]
    private static extern int execvp(string file, string?[] argv);

    // Import fork (to fork a new process) from libc
    [DllImport("libc")]
    private static extern int fork();

    // Import prctl (to set process death signal) from libc
    [DllImport("libc")]
    private static extern int prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);

    // Constants for prctl options
    private const int PR_SET_PDEATHSIG = 1; // Send signal when parent dies
    private const int SIGHUP = 1;           // Signal to send

    public static int Start(string exe, params string[] args)
    {
        Console.WriteLine($"Current pid is {getpid()}");
        Console.WriteLine($"Current session is {getsid(0)}");
        Console.WriteLine($"Current progress group is {getpgid(0)}");

        int err;
        int pid = fork();
        if (pid == -1)
        {
            Console.WriteLine("fork failed");
            Environment.Exit(1);
        }

        if (pid == 0) {
            Console.WriteLine("Child process");
            err = prctl(PR_SET_PDEATHSIG, SIGHUP, 0, 0, 0);
            if (err == -1) {
                Console.WriteLine("prctl failed");
                Environment.Exit(1);
            }

            if (getppid() == 1) // Parent process ID becomes 1 if it has already died
            {
                Console.WriteLine("Parent has already died. Exiting.");
                Environment.Exit(0);
            }

            // Replace the child process with a new executable
            string executable = exe;
            string?[] arguments = [..args, null];

            Console.WriteLine($"Child is executing {executable}...");
            err = execvp(exe, arguments);
            if (err == -1)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"execvp failed. Error code: {error}");
                Environment.Exit(1);
            }
        }

        return pid;
    }
}

