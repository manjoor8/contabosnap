using ContaboSnapshotService;
using ContaboSnapshotService.Configuration;
using ContaboSnapshotService.Services;

var builder = Host.CreateApplicationBuilder(args);

// ── Configuration ────────────────────────────────────────────────
builder.Services.Configure<ContaboSettings>(
    builder.Configuration.GetSection(ContaboSettings.SectionName));

builder.Services.Configure<SnapshotScheduleSettings>(
    builder.Configuration.GetSection(SnapshotScheduleSettings.SectionName));

// ── HTTP Clients ─────────────────────────────────────────────────
builder.Services.AddHttpClient<ContaboAuthService>();
builder.Services.AddHttpClient<ContaboApiService>();

// ── Services ─────────────────────────────────────────────────────
builder.Services.AddSingleton<ContaboAuthService>();
builder.Services.AddSingleton<ContaboApiService>();
builder.Services.AddSingleton<SnapshotManagerService>();

// ── Worker ───────────────────────────────────────────────────────
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
