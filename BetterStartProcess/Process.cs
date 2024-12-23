using System.Runtime.InteropServices;
namespace BetterStartProcess;

//
// Attempt to start a child process and enroll it in the same process group as the parent.
// Goal is to be able to kill the child process if the parent process is killed.
//
public static class Process
{
    [DllImport("libc")]
    private static extern int getpid();

    [DllImport("libc")]
    private static extern int getsid(int pid);

    [DllImport("libc")]
    private static extern int setpgid(int pid, int pgid);

    [DllImport("libc")]
    private static extern int getpgid(int pid);

    [DllImport("libc")]
    private static extern int posix_spawn(
        out int pid,
        string path,
        IntPtr fileActions,
        IntPtr attrp,
        string?[] argv,
        string?[] envp);

    // P/Invoke declaration for posix_spawnattr_init
    [DllImport("libc")]
    private static extern int posix_spawnattr_init(ref IntPtr attrp);

    [DllImport("libc")]
    private static extern int posix_spawnattr_destroy(ref IntPtr attrp);

    [DllImport("libc")]
    private static extern int posix_spawnattr_setpgroup(ref IntPtr attrp, int pgroup);

    [DllImport("libc")]
    private static extern int kill(int pid, int signal);

    // Signal constants
    private const int SIGKILL = 9;

    [DllImport("libc")]
    private static extern IntPtr strerror(int errnum);

    private static void CheckError(string fun, int error)
    {
        if (error != 0)
        {
            IntPtr messagePtr = strerror(error);
            var errstring = Marshal.PtrToStringAnsi(messagePtr) ?? "Unknown error";
            throw new Exception($"{fun} failed with error {error} ({errstring})");
        }
    }

    public static void Kill()
    {
        int err = kill(-getpgid(0), SIGKILL);
        CheckError("kill", err);
    }

    public static int Start(string process, params string[] args)
    {
        Console.WriteLine($"Current pid is {getpid()}");
        Console.WriteLine($"Current session is {getsid(0)}");
        Console.WriteLine($"Current progress group is {getpgid(0)}");

        int err;
        IntPtr attrp = IntPtr.Zero;
        try
        {
            err = setpgid(0, 0);
            CheckError("setpgid", err);

            err = posix_spawnattr_init(ref attrp);
            CheckError("posix_spawnattr_init", err);

            err = posix_spawnattr_setpgroup(ref attrp, 0);
            CheckError("posix_spawnattr_setpgroup", err);

            // Define arguments for the child process
            string path = process;
            string?[] argv = [..args, null];
            string?[] envp = [null]; // Use parent's environment

            // Spawn the child process
            err = posix_spawn(out int childPid, path, IntPtr.Zero, attrp, argv, envp);
            CheckError("posix_spawn", err);

            Console.WriteLine($"Child process started with PID: {childPid}");
            return childPid;
        }
        catch {
            posix_spawnattr_destroy(ref attrp);
            throw;
        }

        // NOTE: also does not work (error 13: Permission denied)
        // // Set the child process to the same process group as the parent
        // err = setpgid(childProcess.Id, 0)
        // if (err != 0)
        // {
        //     Console.WriteLine($"Failed to set process group: {err}");
        //     childProcess.Kill();
        //     return null;
        // }

        // return childProcess;
    }
}

