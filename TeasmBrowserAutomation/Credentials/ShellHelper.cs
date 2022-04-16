// based on: https://jackma.com/2019/04/20/execute-a-bash-script-via-c-net-core/
using System;
using System.Diagnostics;
using System.Threading.Tasks;

#nullable enable

public class ShellResult
{
    public int ExitCode { get; set; }
    public string StdOutput { get; set; } = "";
    public string StdError { get; set; } = "";
    public bool UserCanceled { get; set; }
}

public static class ShellHelper
{
    public static Task<ShellResult> Bash(this string cmd)
    {
        var source = new TaskCompletionSource<ShellResult>();
        var escapedArgs = cmd.Replace("\"", "\\\"");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        process.Exited += (sender, args) =>
          {
              var result = new ShellResult()
              {
                  ExitCode = process.ExitCode,
                  StdError = process.StandardError.ReadToEnd(),
                  StdOutput = process.StandardOutput.ReadToEnd()
              };
              if (process.ExitCode == 0)
              {
                  source.SetResult(result);
              }
              else
              if (process.ExitCode == 1)
              {
                  result.UserCanceled = true;
                  source.SetResult(result);
              }
              else
              {
                  source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
              }

              process.Dispose();
          };

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            source.SetException(e);
        }

        return source.Task;
    }
}