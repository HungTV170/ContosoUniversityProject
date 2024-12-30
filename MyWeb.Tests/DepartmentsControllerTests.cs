using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Models.ViewModels;
using ContosoUniversity.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MyWeb.Tests
{
    public class DepartmentsControllerTests
    {
        private List<Department> GetDepartmentList(){
            return new List<Department>
            {
                new Department { DepartmentID = 1 , Name = "English",     Budget = 350000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 11},
                new Department { DepartmentID = 2 , Name = "Mathematics", Budget = 100000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 12},
            };
        }

        private List<DepartmentViewModel> GetDepartmentVMList(){
            return new List<DepartmentViewModel>
            {
                new DepartmentViewModel { DepartmentID = 1 , Name = "English",     Budget = 350000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 11},
                new DepartmentViewModel { DepartmentID = 2 , Name = "Mathematics", Budget = 100000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 12},
            };
        }
        [Fact]
        public async Task Index_ReturnsViewWithDepartmentViewModels()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();

            var departments = GetDepartmentList();
            var departmentVMs = GetDepartmentVMList();
            mockRepository.Setup(repo => repo.Departments.GetAllAsync(
                null,null,
                It.Is<List<string>>(list => list.Contains("Administrator"))
                )).ReturnsAsync(departments);

            mockMapper.Setup(mapper => mapper.Map<IEnumerable<DepartmentViewModel>>(departments))
                      .Returns(departmentVMs);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<DepartmentViewModel>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
            Assert.Equal("English", model.First().Name);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenDepartmentIsNull()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();

            mockRepository.Setup(repo => repo.Departments.GetTAsync(
                It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 1 }) == true),
                It.Is<List<string>>(list => list.Contains("Administrator"))
                )).ReturnsAsync((Department?)null); 

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Details(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsViewWithDepartment_WhenDepartmentExists()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();

            var department = GetDepartmentList().First(i => i.DepartmentID == 1);
            var departmentVM = GetDepartmentVMList().First(i => i.DepartmentID == 1);


            mockRepository.Setup(c => c.Departments.GetTAsync(
                It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 1 }) == true),
                It.Is<List<string>>(list => list.Contains("Administrator"))
            )).ReturnsAsync(department);

            mockMapper.Setup(mapper => mapper.Map<DepartmentViewModel>(department))
                      .Returns(departmentVM);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);  
            var model = Assert.IsType<DepartmentViewModel>(viewResult.ViewData.Model);  
            Assert.Equal(1, model.DepartmentID);  
            Assert.Equal("English", model.Name); 
        }

        [Fact]
        public async Task Create_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            
            var departmentViewModel = new DepartmentViewModel { DepartmentID = 3 , Name = "English",     Budget = 350000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 11};

            var instructors = new List<Instructor>
            {
                new Instructor { ID = 1, FirstMidName = "John " , LastName = "Doe"},
                new Instructor { ID = 2, FirstMidName = "Jane ", LastName = "Smith"}
            };

            mockRepository.Setup(c => c.Departments.AddAsync(It.IsAny<Department>())).Returns(Task.CompletedTask);
            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);
            mockRepository.Setup(c => c.Instructors.GetAllAsync(
                null,null,null
            )).ReturnsAsync(instructors);

            mockMapper.Setup(m => m.Map<Department>(It.IsAny<DepartmentViewModel>())).Returns(new Department());

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Create(departmentViewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result); 
            Assert.Equal("Index", redirectResult.ActionName); 

            mockRepository.Verify(c => c.Departments.AddAsync(It.IsAny<Department>()), Times.Once);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }


        [Fact]
        public async Task Create_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();

            var departmentViewModel = new DepartmentViewModel { DepartmentID = 3 , Name = "",     Budget = 350000,
                    StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID  = 11};
            // Create a mock list of instructors to return
            var instructors = new List<Instructor>
            {
                new Instructor { ID = 1, FirstMidName = "John " , LastName = "Doe"},
                new Instructor { ID = 2, FirstMidName = "Jane ", LastName = "Smith"}
            };

            mockRepository.Setup(c => c.Instructors.GetAllAsync(
                null,null,null
            )).ReturnsAsync(instructors);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await controller.Create(departmentViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);  
            var model = Assert.IsType<DepartmentViewModel>(viewResult.Model);  
            
            var selectList = Assert.IsType<SelectList>(viewResult.ViewData["InstructorID"]);
            Assert.Equal(2, selectList.Count());  
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenDepartmentExists()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            
            var department = new Department(){
                DepartmentID = 1,
                Name = "Computer Science",
                RowVersion = new byte[] { 0, 1, 2, 3 }
            };

            mockRepository.Setup(r => r.Departments.GetTAsync(
                    It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 1 }) == true),
                    It.IsAny<List<string>>()))
                .ReturnsAsync(department);

            mockRepository.Setup(r => r.Departments.DeleteEntity(It.IsAny<Department>()));

            mockRepository.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.DeleteConfirmed(new DepartmentViewModel { DepartmentID = 1, RowVersion = new byte[] { 0, 1, 2, 3 } });

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenDepartmentDoesNotExist()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            

            mockRepository.Setup(r => r.Departments.GetTAsync(
                    It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 0 }) == true),
                    It.IsAny<List<string>>()))
                .ReturnsAsync((Department?)null);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.DeleteConfirmed(new DepartmentViewModel { DepartmentID = 0 });

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenRowVersionMismatch()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            
            var department = new Department
            {
                DepartmentID = 1,
                Name = "Computer Science",
                RowVersion = new byte[] { 0, 1, 2, 3 }
            };

            var departmentVM = new DepartmentViewModel
            {
                DepartmentID = 1,
                RowVersion = new byte[] { 0, 0, 2, 3 } 
            };

            mockRepository.Setup(r => r.Departments.GetTAsync(
                    It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 1 }) == true),
                    It.IsAny<List<string>>()))
                .ReturnsAsync(department);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.DeleteConfirmed(departmentVM);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirectToActionResult.ActionName);
            bool concurrencyError = (bool)(redirectToActionResult.RouteValues?["concurrencyError"] ?? false);
            Assert.True(concurrencyError, "ConcurrencyError should be true.");
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenExceptionOccurs()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            
            var department = new Department
            {
                DepartmentID = 1,
                Name = "Computer Science",
                RowVersion = new byte[] { 0, 1, 2, 3 }
            };

            var departmentVM = new DepartmentViewModel
            {
                DepartmentID = 1,
                RowVersion = new byte[] { 0, 1, 2, 3 }
            };

            mockRepository.Setup(r => r.Departments.GetTAsync(
                    It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 1 }) == true),
                    It.IsAny<List<string>>()))
                .ReturnsAsync(department);

            mockRepository.Setup(r => r.Departments.DeleteEntity(It.IsAny<Department>()));

            mockRepository.Setup(r => r.SaveAsync()).ThrowsAsync(new DbUpdateException());

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.DeleteConfirmed(departmentVM);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirectToActionResult.ActionName);
            bool concurrencyError = (bool)(redirectToActionResult.RouteValues?["concurrencyError"] ?? false);
            Assert.True(concurrencyError, "ConcurrencyError should be true.");

        }

        [Fact]
        public async Task Edit_ReturnsView_WhenDepartmentDoesNotExist()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();
            
            mockRepository.Setup(r => r.Departments.GetTAsync(
                    It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { DepartmentID = 0 }) == true),
                    It.IsAny<List<string>>()))
                .ReturnsAsync((Department?)null);

            mockRepository.Setup(r => r.Instructors.GetAllAsync(
                null,null,null
            )).ReturnsAsync(
                new List<Instructor>
                {
                    new Instructor { ID = 1, FirstMidName = "John " , LastName = "Doe"},
                    new Instructor { ID = 2, FirstMidName = "Jane ", LastName = "Smith"}
                }
            );
            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Edit(0,new byte[0],new DepartmentViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DepartmentViewModel>(viewResult.Model);

            var selectList = Assert.IsType<SelectList>(viewResult.ViewData["InstructorID"]);
            Assert.Equal(2, selectList.Count());         
        }

        [Fact]
        public async Task Edit_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            // Arrange
             var mockRepository = new Mock<IRepositoryService>();
            var mockMapper = new Mock<IMapper>();

            var existingDepartment = new Department
            {
                DepartmentID = 1,
                Name = "Original Name",
                RowVersion = new byte[] { 1, 2, 3 }
            };

            var updatedViewModel = new DepartmentViewModel
            {
                DepartmentID = 1,
                Name = "Updated Name"
            };


            mockRepository.Setup(repo => repo.Departments.GetTAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<List<string>>()
            )).ReturnsAsync(existingDepartment);

            mockRepository.Setup(repo => repo.Departments.UpdateRowVersion(
                existingDepartment,
                It.IsAny<string>(),
                It.IsAny<byte[]>()
            ));


            mockMapper.Setup(mapper => mapper.Map(updatedViewModel, existingDepartment))
                    .Callback<DepartmentViewModel, Department>((src, dest) => dest.Name = src.Name);


            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);

            var controller = new DepartmentsController(mockRepository.Object, mockMapper.Object);

            // Act
            var result = await controller.Edit(1, new byte[] { 1, 2, 3 }, updatedViewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.Equal("Updated Name", existingDepartment.Name);

            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }

    }
}
