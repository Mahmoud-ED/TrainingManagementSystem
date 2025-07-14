using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Filters;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.Models.Repositories;
using TrainingManagementSystem.Models.UnitOfWork;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();


builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

//-------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>
    (x => x.UseSqlServer(builder.Configuration.GetConnectionString("DbCon")));
//-------------------------------------------------

builder.Services.Configure<ProgrammerSettings>
    (builder.Configuration.GetSection("ProgrammerSettings"));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 3; // ÇáØæá ÇáÃÏäì 3 ÃÍÑÝ
    options.Password.RequiredUniqueChars = 1; // ÍÑÝ ÝÑíÏ æÇÍÏ Úáì ÇáÃÞá
    options.Password.RequireNonAlphanumeric = false; // ÚäÏ ÊÝÚíáåÇ ÓÊÊØáÈ ßáãÇÊ ÇáãÑæÑ ÑãÒ ÎÇÕ æÇÍÏ Úáì ÇáÃÞá
    options.Password.RequireDigit = true; //íÌÈ Ãä ÊÍÊæí Úáì ÑÞã
    options.Password.RequireLowercase = false; //áÇ íáÒã ÃÍÑÝ ÕÛíÑÉ
    options.Password.RequireUppercase = false; //áÇ íáÒã ÃÍÑÝ ßÈíÑÉ
    options.Lockout.MaxFailedAccessAttempts = 5; // ÇÞÝÇá ÇáÍÓÇÈ ÈÚÏ 5 ãÍÇæáÇÊ ÏÎæá ÝÇÔáÉ
    options.Lockout.DefaultLockoutTimeSpan =
             TimeSpan.FromMinutes(15); //ÇÞÝÇá ÇáÍÓÇÈ áãÏÉ ÎãÓÉ ÚÔÑ ÏÞíÞÉ 
    options.SignIn.RequireConfirmedAccount = true; // ÊÓÌíá ÇáÏÎæá íÊØáÈ ÊÃßíÏ ÇáÍÓÇÈ
    options.User.RequireUniqueEmail = true; // ÚäÏ ÇäÔÇÁ ÇáãÓÊÎÏã íÌÈ ÇÓÊÎÏÇã ÈÑíÏ ÅáßÊÑæäí ÝÑíÏ
})
       .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); 

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
                               options.TokenLifespan = TimeSpan.FromMinutes(15));
//opt.TokenLifespan = TimeSpan.FromDays(1));


//-------------------------------------------------


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CreatePolicy", policy => policy.RequireClaim("Create", "true"));
    options.AddPolicy("EditPolicy", policy => policy.RequireClaim("Edit", "true"));
    options.AddPolicy("DeletePolicy", policy => policy.RequireClaim("Delete", "true"));

    options.AddPolicy("EditUserPolicy", policy => policy.RequireClaim("EditUser", "true"));
    options.AddPolicy("SiteStatePolicy", policy => policy.RequireClaim("SiteState", "true"));
    //options.AddPolicy("ProfilePolicy", policy => policy.RequireClaim("UserProfile", "true")); // just example

    options.AddPolicy("AdminOrProgPolicy", policy => policy.RequireRole("Prog", "Admin")); // ÇÐÇ ÇÑÏäÇ ÇÓÊÚãÇá ÇáÑæá ßÈæáíÕí Ëã äØÈÞåÇ Ýí ÇáßæäÊÑæáÑ ßÈæáíÕí


    options.AddPolicy("ProgOrAdminOrEmployeePolicy", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "Admin", "Employee", "Prog"); // ÇÐÇ ÇÑÏäÇ ÇÓÊÚãÇá ÇáÑæá ßÈæáíÕí Ëã äØÈÞåÇ Ýí ÇáßæäÊÑæáÑ ßÈæáíÕí
    });

    options.AddPolicy("SettingsPolicy", policy =>
    {
        policy.RequireClaim("SiteState", "EditUser");
    });


    options.AddPolicy("EditUserPolicy", policy =>
        policy.RequireAssertion(p =>
        p.User.IsInRole("Prog") || (p.User.IsInRole("Admin")
        && p.User.HasClaim(claim => claim.Type == "Edit" && claim.Value == "true")))
    );

});



//------------------------------------------------
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// ...

//appsettings.jason ÊÓÌíá ÞÓã ÇáÅÚÏÇÏÇÊ ÇáÎÇÕÉ ÈÇáÅíãíá æÇáãæÌæÏ Ýí 
var emailconfig = builder.Configuration
                  .GetSection("MailSettings").Get<MailSettings>();
builder.Services.AddSingleton(emailconfig);

//builder.Services.AddScoped<AuditAttribute>();
//builder.Services.AddScoped<AuditActionFilter>();

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddMvc(options =>
{
    options.Filters.Add(typeof(AsyncActionFilter));
}).AddXmlSerializerFormatters();
builder.Services.AddHttpContextAccessor(); //AsyncActionFilter áÅÓÊÚãÇáåÇ áÌáÈ ÇáíæÒÑ ÇáÍÇáí Ýí 

builder.Services.AddHostedService<CourseStatusUpdaterService>();

builder.Services.AddSingleton<UserSessionTracker>();


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}
else if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage(); //ÕÝÍÉ ÇáÎØÃ ÇáÎÇÕÉ ÈÇáãØæÑ



//builder.WebHost.UseKestrel();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.UseAuthentication();//Who are you//ááÊÍÞÞ ãä åæíÉ ÇáãÓÊÎÏã¡ ÇáÇÓã æßáãÉ ÇáãÑæÑ æÇáÑãÒ ÇáÎÇÕ æÇáãÕÇÏÞÉ ÇáËäÇÆíÉ
app.UseAuthorization();//What you allowed to do//ááÊÍÞÞ ãä ÕáÇÍíÉ ÇáãÓÊÎÏã

app.MapControllerRoute(
    name: "default",
     //pattern: "{controller=Home}/{action=Index}/{id?}");
     pattern: "{controller=Account}/{action=Login}/{id?}");


app.Run();
