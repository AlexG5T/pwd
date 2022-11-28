﻿using System.Threading.Tasks;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;

namespace pwd.contexts.session.commands;

public sealed class Open
   : ICommandFactory
{
   private readonly IRepository _repository;
   private readonly IFileFactory _fileFactory;
   private readonly ILock _lock;
   private readonly IState _state;

   public Open(
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state)
   {
      _repository = repository;
      _fileFactory = fileFactory;
      _lock = @lock;
      _state = state;
   }

   public ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "open", var name) =>
            new DelegateCommand(() =>
            {
               var item = _repository.Get(name);
               if (item == null)
                  return;

               var file = _fileFactory.Create(_repository, _lock, item);
               var _ = _state.OpenAsync(file);
            }),
         _ => null
      };
   }
}