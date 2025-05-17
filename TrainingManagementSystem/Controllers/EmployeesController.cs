using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordGenerator;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Controllers;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.Controllers
{
    [Authorize(Roles = "Prog,Admin")]
    [ViewLayout("_LayoutDashboard")]
    public class employeesController : BaseController
    {
        private readonly IUnitOfWork<Employee> _employee;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public employeesController(IUnitOfWork<Employee> employee,
                                   IWebHostEnvironment host,
                                   IEmailSender emailSender,
                                   UserManager<ApplicationUser> userManager,
                                   SignInManager<ApplicationUser> signInManager,
                                   RoleManager<IdentityRole> roleManager) : base(host)
        {
            _employee = employee;
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Employees()
        {
            var employees = await _employee.Entity.GetAll().Include(e => e.ApplicationUser).ToListAsync();

            return View(employees);
        }

        public IActionResult CreateEmployee()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeVM createEmployeeVM)
        {
            if (!ModelState.IsValid)
            {
                return View(createEmployeeVM);
            }

            var user = new ApplicationUser
            {
                UserName = createEmployeeVM.Email,
                Email = createEmployeeVM.Email,
                Age = createEmployeeVM.Age,
                Approval = false,
                CreatedDate = DateTime.UtcNow
            };

            var password = new Password(true, true, true, true, 5);
            string generatedPassword = password.Next();
            var hasher = new PasswordHasher<ApplicationUser>();
            user.PasswordHash = hasher.HashPassword(user, generatedPassword);

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(createEmployeeVM);
            }


            //-------------------------Add Employee row------------------------------------
            var employee = new Employee
            {
                Name = createEmployeeVM.Employee.Name,
                PhoneNumber = createEmployeeVM.Employee.PhoneNumber,
                Address = createEmployeeVM.Employee.Address,
                YearsOfExperience = createEmployeeVM.Employee.YearsOfExperience,
                Specialization = createEmployeeVM.Employee.Specialization,
                Bio = createEmployeeVM.Employee.Bio,
                UserId = user.Id,
                Created = DateTime.UtcNow
            };

            try
            {
                _employee.Entity.Insert(employee);
                await _employee.SaveAsync();
            }
            catch
            {
                return View("Error");
            }

            //-------------------------Add Role EmployeeRequest------------------------------------
            if (!await _roleManager.RoleExistsAsync("EmployeeRequest "))
            {
                var employeeRequestRole = new IdentityRole
                {
                    Name = "EmployeeRequest ",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                if (!(await _roleManager.CreateAsync(employeeRequestRole)).Succeeded)
                {
                    TempData["ErrorMessage"] = "The Role: '" + employeeRequestRole.Name + "' failed to be added";
                    return View("NotFound");
                }
            }

            //-----------------------Add User To Role Employee-------------------------------
            if (!(await _userManager.AddToRoleAsync(user, "EmployeeRequest")).Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to add user to Role: 'EmployeeRequest'";
                return View("NotFound");
            }
            //------------------------Send email---------------------------------------------
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("EmailConfirm", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                string content = ReadHtmlTemplate("ConfirmEmailWithPassword.html");
                string subject = "Email Confirmation";
                content = content.Replace("{Subject}", subject);
                content = content.Replace("{UserName}", createEmployeeVM.Email);
                content = content.Replace("{Password}", generatedPassword);
                content = content.Replace("{confirmationLink}", confirmationLink);

                var message = new Message(new string[] { createEmployeeVM.Email }, subject, content, null);

                try
                {
                    await _emailSender.SendEmailAsync(message);
                    TempData["SuccessMessage"] = "The email has been sent successfully";
                    return RedirectToAction("Employees");
                }
                catch
                {
                    TempData["ErrorMessage"] = "Failed to send email";
                    return View(createEmployeeVM);
                }
            }

        }


        public async Task<IActionResult> EditEmployee(Guid? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var employee = await _employee.Entity.GetWhere(e => e.Id == id)
                                                 .Include(u => u.ApplicationUser)
                                                 .FirstOrDefaultAsync();
            if (employee == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found employee with Id={id}";
                return View("NotFound");
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> EditEmployee([Bind("Id,Name,PhoneNumber,Address,YearsOfExperience,Specialization,Bio,UserId,Created")] Employee employee)
        {
            //المستخدم ب البوتون Id لا نحتاج له في التعديل ولكن نحتاج له عند ارجاعه الى نفس الصفحة لعرض ايميل المستخدم وربط ApplicationUser=null لأنه سيرجع
            //-----------------------------------------
            var applicationUser = await _userManager.FindByIdAsync(employee.UserId);
            if (applicationUser == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={employee.UserId}";
                return View("NotFound");
            }

            //var applicationUser = await _userManager.Users.Include(p => p.UserProfile)
            //                 .Where(u => u.Id == employee.UserId)
            //                 .FirstOrDefaultAsync();

            employee.ApplicationUser = applicationUser;
            //--------------------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            var name = await _employee.Entity.GetWhere(e => e.Id != employee.Id & e.Name == employee.Name).Select(e => e.Name).FirstOrDefaultAsync();
            if (name != null)
            {
                TempData["ErrorMessage"] = "The Employee name is reserved";
                return View(employee);
            }

            try
            {
                employee.Modified = DateTime.UtcNow;
                _employee.Entity.Update(employee);
                await _employee.SaveAsync();
            }
            catch (Exception)
            {
                return View("Error");
            }

            TempData["SuccessMessage"] = "Saved successfully";
            return RedirectToAction("EditEmployee", new { employee.Id });
        }




        //[Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> ApprovalEmployee(string? userId)
        {
            if (userId == null)
            {
                return View("NotFound");
            }

            var user = await _userManager.Users.Where(u => u.Id == userId)
                                               .Include(e=>e.Employee)
                                               .FirstOrDefaultAsync();
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={userId}";
                return View("NotFound");
            }

           
            user.Approval =true;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                //-------------------------Add Role Employee------------------------------------
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    var employeeRole = new IdentityRole
                    {
                        Name = "Employee",
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };

                    if (!(await _roleManager.CreateAsync(employeeRole)).Succeeded)
                    {
                        ViewBag.ErrorMessage = "The Role: '" + employeeRole.Name + "' failed to be added";
                        return View("NotFound");
                    }
                }

                //-----------------------Add User To Role Employee-------------------------------
                if (!await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    if (!(await _userManager.AddToRoleAsync(user, "Employee")).Succeeded)
                    {
                        ViewBag.ErrorMessage = "Failed to add user to Role: 'Employee'";
                        return View("Error");
                    }
                }
                //------------------------------------------------------------------------------
                if (await _userManager.IsInRoleAsync(user, "EmployeeRequest"))
                {
                    if (!(await _userManager.RemoveFromRoleAsync(user, "EmployeeRequest")).Succeeded)
                    {
                        ViewBag.ErrorMessage = "Failed to add user to Role: 'Employee'";
                        return View("Error");
                    }
                }
                    
                //------------------------------------------------------------------------------


                TempData["SuccessMessage"] = "Approval has been given to the employee";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    //ModelState.AddModelError(string.Empty, error.Description); // RedirectToAction لا تظهر لأنه لا يبقى في نفس الصفحة لأنه يقوم بتحميل الصفحة من جديد عن طريق
                    TempData["ErrorMessage"] = error.Description;
                }
            }


            var refererUrl = HttpContext.Request.Headers["Referer"].ToString(); // جلب اللنك الكامل للصفحة السابقة التي تم الاستدعاء منها
            if (refererUrl != null && refererUrl.Contains("EditEmployee"))
            {
                return RedirectToAction("EditEmployee", new { id = user.Employee.Id });
            }
            else if (refererUrl != null && refererUrl.Contains("Employees"))
            {
                return RedirectToAction("Employees");
            }
            else
            {
                return View("NotFound");
            }


        }




    }
}
