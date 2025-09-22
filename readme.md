## Important Command

```bash
mkdir ChatBot
cd ChatBot
dotnet new web
cd ..
dotnet sln add ChatBot
cd DotNetChatbot.AppHost
dotnet add reference ../ChatBot
cd ..
cd ChatBot
dotnet add reference ../DotNetChatbot.ServiceDefaults
```

Nicht vergessen: Auf .NET 10 umstellen

