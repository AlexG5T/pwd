﻿#r "../../bin/Release/net8.0/publish/pwd.core.dll"

open pwd.core

let repository = FolderRepository.Open ("$9cre7", ".")
repository.Write ("/test", "content")
