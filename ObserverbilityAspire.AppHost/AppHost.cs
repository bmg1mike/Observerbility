var builder = DistributedApplication.CreateBuilder(args);



var compose = builder.AddDockerComposeEnvironment("observability")
                     .WithDashboard(dashboard =>
                     {
                         dashboard.WithHostPort(8081).WithForwardedHeaders(enabled: true);
                     });

var seq = builder.AddSeq("seq")
                 .ExcludeFromManifest()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithEnvironment("ACCEPT_EULA", "Y");

var todoApi = builder.AddProject<Projects.Todos_Api>("TodoApi")
              .PublishAsDockerComposeService((resource, service) =>
              {
                  service.Name = "todo-api";
              })
              .WithReference(seq)
              .WithHttpHealthCheck("/health")
              .WaitFor(seq);

builder.Build().Run();
