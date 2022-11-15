using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.context;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.contexts.session.commands;

namespace pwd.contexts.session;

public interface ISession
   : IContext
{
}

public interface ISessionFactory
{
   ISession Create(
      IRepository repository,
      IExporter exporter,
      ILock @lock);
}


/// <summary>Repository working session context.</summary>
public sealed class Session
   : Repl,
      ISession
{
   private readonly ILogger _logger;
   private readonly IRepository _repository;
   private readonly IReadOnlyCollection<ICommandFactory> _factories;

   public Session(
      ILogger logger,
      IRepository repository,
      IView view,
      IReadOnlyCollection<ICommandFactory> factories)
      : base(
         logger,
         view)
   {
      _logger = logger;
      _repository = repository;

      _factories = factories;
   }

   public override async Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      _logger.Info($"Session.ProcessAsync({input})");

      var command =
         _factories
            .Select(item => item.Parse(input))
            .FirstOrDefault(item => item != null);

      if (command == null)
         return;

      await command.DoAsync(cancellationToken);
   }

   public override (int, IReadOnlyList<string>) Get(
      string input)
   {
      if (!input.StartsWith('.'))
      {
         var p = input.LastIndexOf('/');
         var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
         return (
            input.Length,
            _repository.List(folder == "" ? "." : folder)
               .Where(item => item.Path.StartsWith(input))
               .Select(item => item.Path)
               .ToArray());
      }

      return (input.Length, new[]
         {
            ".add",
            ".archive",
            ".clear",
            ".export",
            ".lock",
            ".pwd",
            ".quit"
         }
         .Where(item => item.StartsWith(input))
         .ToArray());
   }
}

public sealed class SessionFactory
   : ISessionFactory
{
   private readonly IFileFactory _fileFactory;
   private readonly INewFileFactory _newFileFactory;
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;

   public SessionFactory(
      ILogger logger,
      IState state,
      IView view,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory)
   {
      _logger = logger;
      _state = state;
      _view = view;
      _fileFactory = fileFactory;
      _newFileFactory = newFileFactory;
   }

   public ISession Create(
      IRepository repository,
      IExporter exporter,
      ILock @lock)
   {
      return new Session(
         _logger,
         repository,
         _view,
         Array.Empty<ICommandFactory>()
            .Concat(
               new ICommandFactory[]
               {
                  new Add(_state, _newFileFactory, repository),
                  new Export(_view),
                  new Help(_view),
                  new Html(exporter),
                  new Open(repository, _fileFactory, @lock, _state)
               })
            .Concat(Shared.CommandFactories(_state, @lock, _view))
            .Concat(new ICommandFactory[] { new List(repository, _fileFactory, @lock, _state, _view) })
            .ToArray());
   }
}