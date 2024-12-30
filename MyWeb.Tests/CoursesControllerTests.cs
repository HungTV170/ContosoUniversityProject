using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using ContosoUniversity.Authorization;
using ContosoUniversity.Controllers;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Models.ViewModels;
using ContosoUniversity.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
namespace MyWeb.Tests
{
    public class CoursesControllerTests
    {
        private readonly Mock<IRepositoryService> mockRepository;
        private readonly Mock<IMapper> mockMapper;

        private readonly Mock<IAuthorizationService> mockAuthorization ;

        private readonly Mock<UserManager<ContosoUser>> mockUserManager ;

        private readonly Mock<ClaimsPrincipal> mockUser; 
        private readonly CoursesController controller;

        public CoursesControllerTests()
        {

            var mockUserStore = new Mock<IUserStore<ContosoUser>>();

            mockUser = new Mock<ClaimsPrincipal>();
            mockRepository = new Mock<IRepositoryService>();
            mockMapper = new Mock<IMapper>();
            mockAuthorization = new Mock<IAuthorizationService>();

            mockUserManager = new Mock<UserManager<ContosoUser>>(
                new Mock<IUserStore<ContosoUser>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ContosoUser>>().Object,
                new IUserValidator<ContosoUser>[0],
                new IPasswordValidator<ContosoUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ContosoUser>>>().Object);

            controller = new CoursesController(
                mockRepository.Object, 
                mockAuthorization.Object, 
                mockUserManager.Object,
                mockMapper.Object
            ){
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = mockUser.Object }
                }
            };
        }
        // Data 
        private List<Course> coursesMock = new List<Course>
        {
            new Course { CourseID = 1, Title = "Course 1", Status = ContactStatus.Approved, OwnerID = "user123" },
            new Course { CourseID = 2, Title = "Course 2", Status = ContactStatus.Submitted, OwnerID = "user456" }
        }; 

        private List<CourseViewModel> courseViewModelsMock = new List<CourseViewModel>
        {
            new CourseViewModel { CourseID = 1, Title = "Course 1 vm", Status = ContactStatus.Approved, OwnerID = "user123" },
            new CourseViewModel { CourseID = 2, Title = "Course 2 vm", Status = ContactStatus.Submitted, OwnerID = "user456" }
        };
        // Add tests here

        [Fact]
        public async Task Index_UserIsNotAuthorized_FiltersCourses()
        {
            // Arrange
            var currentUserId = "user123";
        

            mockRepository.Setup(repo => repo.Courses.GetAllAsync(
                null,null,
                 It.IsAny<List<string>>()
            )).ReturnsAsync(coursesMock);

            mockMapper.Setup(mapper => mapper.Map<IEnumerable<CourseViewModel>>(
                    It.IsAny<IEnumerable<Course>>()))
                .Returns((IEnumerable<Course> source) =>
                {
                    if (source == null)
                    {
                        return new List<CourseViewModel>();
                    }
                    return source.Select(course => new CourseViewModel
                    {
                        CourseID = course.CourseID,
                        Title = course.Title,
                        Status = course.Status,
                        OwnerID = course.OwnerID
                    }).ToList();
                });

            mockUserManager.Setup(um => um.GetUserId(
                It.IsAny<ClaimsPrincipal>()
            )).Returns(currentUserId);
            
            mockUser.Setup(u => u.IsInRole(ContosoResource.ContosoAdministratorsRole)).Returns(false);
            mockUser.Setup(u => u.IsInRole(ContosoResource.ContosoManagersRole)).Returns(false);
            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CourseViewModel>>(viewResult.Model);
            Assert.Single(model);  
        }

        [Fact]
        public async Task Index_UserIsAuthorized_ReturnsAllCourses()
        {
            // Arrange
            var currentUserId = "user123";
        

            mockRepository.Setup(repo => repo.Courses.GetAllAsync(
                null,null,
                It.IsAny<List<string>>()
            )).ReturnsAsync(coursesMock);

            mockMapper.Setup(mapper => mapper.Map<IEnumerable<CourseViewModel>>(
                    It.IsAny<IEnumerable<Course>>()))
                .Returns((IEnumerable<Course> source) =>
                {
                    if (source == null)
                    {
                        return new List<CourseViewModel>();
                    }
                    return source.Select(course => new CourseViewModel
                    {
                        CourseID = course.CourseID,
                        Title = course.Title,
                        Status = course.Status,
                        OwnerID = course.OwnerID
                    }).ToList();
                });

            mockUserManager.Setup(um => um.GetUserId(
                It.IsAny<ClaimsPrincipal>()
            )).Returns(currentUserId);
            
            mockUser.Setup(u => u.IsInRole(ContosoResource.ContosoAdministratorsRole)).Returns(true);
            mockUser.Setup(u => u.IsInRole(ContosoResource.ContosoManagersRole)).Returns(false);
            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CourseViewModel>>(viewResult.Model);
            Assert.Equal(2,model.Count());     
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange

            // Act
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenCourseIsNull()
        {
            // Arrange

            mockRepository.Setup(repo => repo.Courses.GetTAsync(
                It.Is<Expression<Func<Course, bool>>>(expr => expr.Compile().Invoke(new Course { CourseID = 3 }) == true),
                It.Is<List<string>>(list => list.Contains("Department"))
                )).ReturnsAsync((Course?)null); 

            // Act
            var result = await controller.Details(3);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsViewWithCourse_WhenCourseExists()
        {
            // Arrange

            var mockCourse = coursesMock.First(i => i.CourseID == 1);
            var mockCourseViewModel = courseViewModelsMock.First(i => i.CourseID == 1);


            mockRepository.Setup(repo => repo.Courses.GetTAsync(
                It.Is<Expression<Func<Course, bool>>>(expr => expr.Compile().Invoke(new Course { CourseID = 1 }) == true),
                It.Is<List<string>>(list => list.Contains("Department"))
                )).ReturnsAsync(mockCourse); 

            mockMapper.Setup(mapper => mapper.Map<CourseViewModel>(mockCourse))
                .Returns((Course source) =>
                {
                    if (source == null)
                    {
                        return new CourseViewModel();
                    }
                    return new CourseViewModel
                    {
                        CourseID = source.CourseID,
                        Title = source.Title,
                        Status = source.Status,
                        OwnerID = source.OwnerID
                    };
                });

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);  
            var model = Assert.IsType<CourseViewModel>(viewResult.ViewData.Model);  
            Assert.Equal(1, model.CourseID);  
            Assert.Equal("Course 1", model.Title); 
        }

        [Fact]
        public async Task DetailsPost_UpdatesCourseAndReturnRedirectsToIndex_WithValidModelState()
        {
            // Arrange
            var courseId = 1;
            var mockCourse = coursesMock.First(i => i.CourseID == courseId);
            var mockCourseViewModel = courseViewModelsMock.First(i => i.CourseID == courseId);

            mockRepository.Setup(repo => repo.Courses.GetTAsync(
                It.Is<Expression<Func<Course, bool>>>(expr => expr.Compile().Invoke(new Course { CourseID = 1 }) == true), 
                null
            )).ReturnsAsync(mockCourse);

            mockMapper.Setup(mapper => mapper.Map(It.IsAny<CourseViewModel>(), It.IsAny<Course>()))
                    .Callback<CourseViewModel, Course>((vm, s) => 
                    {
                        s.CourseID = vm.CourseID;
                        s.Title = vm.Title?? "No Title";
                        s.Status = vm.Status;
                        s.OwnerID = vm.OwnerID;
                    });

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Success());

            mockRepository.Setup(repo => repo.Courses.Update(mockCourse));
            mockRepository.Setup(repo => repo.SaveAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await controller.DetailsPost(courseId, mockCourseViewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(controller.Index), redirectResult.ActionName);

            mockRepository.Verify(repo => repo.Courses.Update(mockCourse), Times.Once);
            mockRepository.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DetailsPost_ReturnViewWithModelError_WhenModelStateInvalid()
        {
            // Arrange
            var courseId = 1;
            var courseViewModel = new CourseViewModel { CourseID = courseId };
            controller.ModelState.AddModelError("error", "Invalid model");

            mockRepository.Setup(repo => repo.Departments.GetAllAsync(
                null,null,null
            )).ReturnsAsync(new List<Department>(){
                new Department(){
                    DepartmentID =  1,
                    Name =  "Departmetn 1"
                },
                new Department(){
                    DepartmentID =  2,
                    Name =  "Departmetn 2"
                },
            });

            // Act
            var result = await controller.DetailsPost(courseId, courseViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);

            var selectList = Assert.IsType<SelectList>(viewResult.ViewData["DepartmentID"]);
            Assert.Equal(2, selectList.Count());  
        }


        [Fact]
        public async Task DetailsPost_ReturnForbid_WhenAuthorizationFailed()
        {
            // Arrange
            var courseId = 1;
            var mockCourse = coursesMock.First(i => i.CourseID == courseId);
            var mockCourseViewModel = courseViewModelsMock.First(i => i.CourseID == courseId);

            mockRepository.Setup(repo => repo.Courses.GetTAsync(
                It.Is<Expression<Func<Course, bool>>>(expr => expr.Compile().Invoke(new Course { CourseID = 1 }) == true), 
                null
            )).ReturnsAsync(mockCourse);

            mockMapper.Setup(mapper => mapper.Map(It.IsAny<CourseViewModel>(), It.IsAny<Course>()))
                    .Callback<CourseViewModel, Course>((vm, s) => 
                    {
                        s.CourseID = vm.CourseID;
                        s.Title = vm.Title?? "No Title";
                        s.Status = vm.Status;
                        s.OwnerID = vm.OwnerID;
                    });
            
            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await controller.DetailsPost(courseId, mockCourseViewModel);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_ReturnForbid_WhenUserNotAuthorized()
        {

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Failed());

            mockMapper.Setup(mapper => mapper.Map<Course>(
                It.IsAny<CourseViewModel>()))
            .Returns(new Course());

            // Act
            var result = await controller.Create(new CourseViewModel());

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_ReturnView_WhenUserAuthorizedAndModelStateValid()
        {
            // Act
            mockMapper.Setup(mapper => mapper.Map<Course>(
                It.IsAny<CourseViewModel>()))
            .Returns((CourseViewModel course) => {
                return new Course(){
                    CourseID = course.CourseID,
                    Title = course.Title ?? "No Title",
                    Status = course.Status,
                    OwnerID = course.OwnerID 
                };
            });

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Success());

            var currentUserId = "creatUser123";
            mockUserManager.Setup(um => um.GetUserId(
                It.IsAny<ClaimsPrincipal>()
            )).Returns(currentUserId);

            mockRepository.Setup(repo => repo.Courses.AddAsync(It.Is<Course>(c => c.CourseID == 1 && c.OwnerID!.Equals("currentUserId"))));
            mockRepository.Setup(repo => repo.SaveAsync()).Returns(Task.CompletedTask);

            var result = await controller.Create(courseViewModelsMock.Find(c => c.CourseID == 1 )!);

            // Assert
            var viewResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", viewResult.ActionName);
            mockRepository.Verify(repo => repo.Courses.AddAsync(It.IsAny<Course>()), Times.Once);
            mockRepository.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnForbid_WhenUserNotAuthorized()
        {

            var courseId = 1;

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Failed());

            mockRepository.Setup(m => m.Courses.GetTAsync(
                It.IsAny<Expression<Func<Course, bool>>>(), 
                null
            )).ReturnsAsync( new Course { CourseID = courseId });

            // Act
            var result = await controller.DeleteConfirmed(courseId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnRedirectToIndex_WhenCourseNotFound()
        {
            var courseId = 1;

            mockRepository.Setup(m => m.Courses.GetTAsync(
                It.IsAny<Expression<Func<Course, bool>>>(), null
            )).ReturnsAsync((Course?)null);

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Success());

            // Act
            var result = await controller.DeleteConfirmed(courseId);
            var viewResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", viewResult.ActionName);
            mockRepository.Verify(repo => repo.Courses.DeleteEntity(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnRedirectToIndex_WhenDeleteSuccess()
        {
            var courseId = 1;

            mockRepository.Setup(m => m.Courses.GetTAsync(
                It.IsAny<Expression<Func<Course, bool>>>(), null
            )).ReturnsAsync(new Course { CourseID = courseId });

            mockAuthorization.Setup(auth => auth.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<object>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
            )).ReturnsAsync(AuthorizationResult.Success());


            mockRepository.Setup(m => m.Courses.DeleteEntity(It.IsAny<Course>()));
            mockRepository.Setup(m => m.SaveAsync());

            // Act
            var result = await controller.DeleteConfirmed(courseId);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            mockRepository.Verify(repo => repo.Courses.DeleteEntity(It.Is<Course>(c => c.CourseID == 1 )), Times.Once);
            mockRepository.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Edit_ReturnNotFound_WhenIdMismatch()
        {
            var id = 1;
            var courseViewModel = new CourseViewModel { CourseID = 2 };
            var result = await controller.Edit(id, courseViewModel);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ReturnViewWithModel_WhenModelStateInvalid()
        {
            var id = 1;
            var courseViewModel = new CourseViewModel { CourseID = id };

            controller.ModelState.AddModelError("Error", "Model is invalid");

            mockRepository.Setup(repo => repo.Departments.GetAllAsync(
                null,null,null
            )).ReturnsAsync(new List<Department>(){
                new Department(){
                    DepartmentID =  1,
                    Name =  "Departmetn 1"
                },
                new Department(){
                    DepartmentID =  2,
                    Name =  "Departmetn 2"
                },
            });

            var result = await controller.Edit(id, courseViewModel);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(courseViewModel, viewResult.Model);

            var selectList = Assert.IsType<SelectList>(viewResult.ViewData["DepartmentID"]);
            Assert.Equal(2, selectList.Count()); 
        }

        [Fact]
        public async Task Edit_ReturnNotFound_WhenCourseNotFound()
        {
            var id = 1;
            var courseViewModel = new CourseViewModel { CourseID = id };

            mockRepository.Setup(m => m.Courses.GetTAsync(
                It.IsAny<Expression<Func<Course, bool>>>(), null
            )).ReturnsAsync((Course?) null);

            var result = await controller.Edit(id, courseViewModel);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ReturnRedirectToIndex_SaveChangesSuccess()
        {

            var id = 1;
            var course = coursesMock.Find(c  => c.CourseID == id  );

            mockRepository.Setup(m => m.Courses.GetTAsync(
                It.IsAny<Expression<Func<Course, bool>>>(), null
            )).ReturnsAsync(course);
            
            mockMapper.Setup(m => m.Map(It.IsAny<CourseViewModel>(), It.IsAny<Course>()))
                .Callback<CourseViewModel, Course>((vm, c) => 
                    { 
                        c.CourseID = vm.CourseID; 
                        c.Title = vm.Title!;
                        c.Status = vm.Status;
                        c.OwnerID = vm.OwnerID;
                    }
                ); 

            mockRepository.Setup(m => m.Courses.Update(It.IsAny<Course>()));
            mockRepository.Setup(m => m.SaveAsync());

            var result = await controller.Edit(id, courseViewModelsMock.Find(c  => c.CourseID == id)!);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            mockRepository.Verify(m => m.Courses.Update(It.Is<Course>(c  => c.CourseID == id && c.Title.Equals("Course 1 vm"))), Times.Once);
            mockRepository.Verify(m => m.SaveAsync(), Times.Once);           
        }
    }
}