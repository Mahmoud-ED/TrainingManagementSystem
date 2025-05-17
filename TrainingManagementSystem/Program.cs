using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.Models.Repositories;
using TrainingManagementSystem.Models.UnitOfWork;
using System.Security.Claims;



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
    options.Password.RequiredLength = 3; // ����� ������ 3 ����
    options.Password.RequiredUniqueChars = 1; // ��� ���� ���� ��� �����
    options.Password.RequireNonAlphanumeric = false; // ��� ������� ������ ����� ������ ��� ��� ���� ��� �����
    options.Password.RequireDigit = true; //��� �� ����� ��� ���
    options.Password.RequireLowercase = false; //�� ���� ���� �����
    options.Password.RequireUppercase = false; //�� ���� ���� �����
    options.Lockout.MaxFailedAccessAttempts = 5; // ����� ������ ��� 5 ������� ���� �����
    options.Lockout.DefaultLockoutTimeSpan =
             TimeSpan.FromMinutes(15); //����� ������ ���� ���� ��� ����� 
    options.SignIn.RequireConfirmedAccount = true; // ����� ������ ����� ����� ������
    options.User.RequireUniqueEmail = true; // ��� ����� �������� ��� ������� ���� �������� ����
})
       .AddEntityFrameworkStores<ApplicationDbContext>()
    //������ �������� ������ ��� ������� ������ ����������� Identity ���
    .AddDefaultTokenProviders(); //������� ���� ������ ��������� ������ ���� ����� ����� ���� ������ ������ ������ ���������� �������� ��� ������ ���������� ������� ��������� �������� .


//����� ��� ���� ������� �� ������� ������� ��� �������//DataProtectorTokenProvider//�� ���� ������ ���� ��� ������� ������ ���� ������
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

    options.AddPolicy("AdminOrProgPolicy", policy => policy.RequireRole("Prog", "Admin")); // ��� ����� ������� ����� ������� �� ������ �� ���������� �������


    options.AddPolicy("ProgOrAdminOrEmployeePolicy", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "Admin", "Employee", "Prog"); // ��� ����� ������� ����� ������� �� ������ �� ���������� �������
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

//appsettings.jason ����� ��� ��������� ������ �������� �������� �� 
var emailconfig = builder.Configuration
                  .GetSection("MailSettings").Get<MailSettings>();
builder.Services.AddSingleton(emailconfig);

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddMvc(options =>
{
    options.Filters.Add(typeof(AsyncActionFilter));
}).AddXmlSerializerFormatters();
builder.Services.AddHttpContextAccessor(); //AsyncActionFilter ���������� ���� ������ ������ �� 


builder.Services.AddSingleton<UserSessionTracker>();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}
else if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage(); //���� ����� ������ �������


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();//Who are you//������ �� ���� �������� ����� ����� ������ ������ ����� ��������� ��������
app.UseAuthorization();//What you allowed to do//������ �� ������ ��������

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");




app.Run();
