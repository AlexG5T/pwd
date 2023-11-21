﻿using System.IO.Abstractions;

namespace pwd.repository.implementation;

public sealed class RepositoryFactory(
      ILogger logger,
      IFileSystem fs,
      ICipherFactory cipherFactory,
      IEncoder encoder)
   : IFactory
{
   public IRepository Create(
      string password,
      string path)
   {
      return new Repository(
         logger,
         fs,
         cipherFactory.Create(password),
         encoder,
         path);
   }
}