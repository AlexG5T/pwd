using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace pwd;

public interface IClipboard
{
   /// <summary>Put the text to the clipboard and clear it after specified time.</summary>
   void Put(
      string text,
      TimeSpan clearAfter);

   /// <summary>Replace the clipboard content with an empty string.</summary>
   void Clear();
}

public sealed class Clipboard
   : IClipboard,
      IDisposable
{
   private readonly ILogger _logger;
   private readonly Channel<string> _channel;
   private readonly Timer _cleaner;
   private readonly CancellationTokenSource _cts;

   public Clipboard(
      ILogger logger)
   {
      _logger = logger;

      _cleaner = new(_ => Clear());

      _cts = new();

      var token = _cts.Token;

      _channel = Channel.CreateUnbounded<string>();
      Task.Run(async () =>
      {
         var reader = _channel.Reader;
         while (!reader.Completion.IsCompleted && !token.IsCancellationRequested)
         {
            string text;
            try
            {
               text = await reader.ReadAsync(token);
            }
            catch (OperationCanceledException e)
            {
               if (e.CancellationToken == token)
                  break;
               throw;
            }

            CopyText(text);
         }
      });
   }

   public void Put(
      string text,
      TimeSpan clearAfter)
   {
      _cleaner.Change(clearAfter, Timeout.InfiniteTimeSpan);
      _channel.Writer.TryWrite(text);
   }

   public void Clear()
   {
      _channel.Writer.TryWrite("");
      _cleaner.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
   }

   public void Dispose()
   {
      _cts.Cancel();
      _cts.Dispose();

      _channel.Writer.Complete();
   }

   private void CopyText(
      string text)
   {
      Exception? Run(
         string executable)
      {
         try
         {
            var startInfo = new ProcessStartInfo(executable)
            {
               RedirectStandardInput = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
               throw new($"Starting the executable '{executable}' failed.");

            var stdin = process.StandardInput;
            stdin.Write(text);
            stdin.Close();
         }
         catch (Exception e)
         {
            return e;
         }

         return null;
      }

      if (Run("clip.exe") is not null &&
          Run("pbcopy") is not null &&
          Run("xsel") is not null)
      {
         _logger.Error("Cannot copy to the clipboard.");
      }
   }
}