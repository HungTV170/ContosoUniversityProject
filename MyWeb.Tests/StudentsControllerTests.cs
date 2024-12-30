using System.Linq.Expressions;
using AutoMapper;
using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Models.ViewModels;
using ContosoUniversity.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace MyWeb.Tests
{
    public class StudentsControllerTests{
        private readonly Mock<IRepositoryService> mockRepository;
        private readonly Mock<IMapper> mockMapper;
        private readonly StudentsController controller;
        
        public StudentsControllerTests(){
            mockRepository = new Mock<IRepositoryService>();
            mockMapper = new Mock<IMapper>();
            controller = new StudentsController(mockRepository.Object, mockMapper.Object);
        }
        // Data
        private List<Student> mockStudents = new List<Student>{
            new Student{ID=1, LastName="Smith", FirstMidName="John", EnrollmentDate=DateTime.Parse("2020-09-01")},
            new Student{ID=2, LastName="Doe", FirstMidName="Jane", EnrollmentDate=DateTime.Parse("2020-09-01")}
        };

        private List<StudentViewModel> mockStudentViewModels = new List<StudentViewModel>{
            new StudentViewModel{ID=1, LastName="Smith vm", FirstMidName="John", EnrollmentDate=DateTime.Parse("2020-09-01")},
            new StudentViewModel{ID=2, LastName="Doe vm", FirstMidName="Jane", EnrollmentDate=DateTime.Parse("2020-09-01")}
        };

        // Add tests here

        [Fact]
        public async Task Index_ReturnsView_WithStudentViewModels()
        {
            // Arrange;
            mockRepository.Setup(repo => repo.Students.GetAllAsync(
                    It.IsAny<Expression<Func<Student, bool>>>(), 
                    It.IsAny<Func<IQueryable<Student>, IOrderedQueryable<Student>>>(), 
                    It.IsAny<List<string>>()))
                .ReturnsAsync(mockStudents);

            mockMapper.Setup(mapper => mapper.Map<IEnumerable<StudentViewModel>>(
                    It.IsAny<IEnumerable<Student>>()))
                .Returns(mockStudentViewModels);
            // Act
            var result = await controller.Index("","",null,"");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentViewModel>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
            Assert.Equal("Smith vm", model.First().LastName);
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
        public async Task Details_ReturnsNotFound_WhenStudentIsNull()
        {
            // Arrange

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                It.Is<Expression<Func<Student, bool>>>(expr => expr.Compile().Invoke(new Student { ID = 3 }) == true),
                It.Is<List<string>>(list => list.Contains("Enrollments.Course"))
                )).ReturnsAsync((Student?)null); 

            // Act
            var result = await controller.Details(3);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsViewWithStudent_WhenStudentExists()
        {
            // Arrange

            var mockStudent = mockStudents.First(i => i.ID == 1);
            var mockStudentViewModel = mockStudentViewModels.First(i => i.ID == 1);


            mockRepository.Setup(c => c.Students.GetTAsync(
                It.Is<Expression<Func<Student, bool>>>(expr => expr.Compile().Invoke(new Student { ID = 1 }) == true),
                It.Is<List<string>>(list => list.Contains("Enrollments.Course"))
            )).ReturnsAsync(mockStudent);

            mockMapper.Setup(mapper => mapper.Map<StudentViewModel>(mockStudent))
                      .Returns(mockStudentViewModel);

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);  
            var model = Assert.IsType<StudentViewModel>(viewResult.ViewData.Model);  
            Assert.Equal(1, model.ID);  
            Assert.Equal("Smith vm", model.LastName); 
        }

        [Fact]
        public async Task Create_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            // Arrange
            


            mockRepository.Setup(c => c.Students.AddAsync(It.IsAny<Student>())).Returns(Task.CompletedTask);
            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);


            mockMapper.Setup(m => m.Map<Student>(It.IsAny<StudentViewModel>())).Returns(new Student());


            // Act
            var result = await controller.Create(new StudentViewModel());

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result); 
            Assert.Equal("Index", redirectResult.ActionName); 

            mockRepository.Verify(c => c.Students.AddAsync(It.IsAny<Student>()), Times.Once);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange

            controller.ModelState.AddModelError("LastName", "The LastName field is required.");

            // Act
            var result = await controller.Create(new StudentViewModel(){
                LastName = "",
                FirstMidName = "John",
                EnrollmentDate = DateTime.Parse("2020-09-01")
            });

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);  
            var model = Assert.IsType<StudentViewModel>(viewResult.Model);         
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesStudent_AndRedirectsToIndex()
        {
            // Arrange
            int studentId = 1;
            var student = new Student { ID = studentId, FirstMidName = "John", LastName = "Doe" };

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                    It.IsAny<Expression<Func<Student, bool>>>(),
                    It.IsAny<List<string>>()
                )).ReturnsAsync(student);

            mockRepository.Setup(repo => repo.Students.DeleteEntity(It.IsAny<Student>()));

            mockRepository.Setup(repo => repo.SaveAsync());

            // Act
            var result = await controller.DeleteConfirmed(studentId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName); 

            mockRepository.Verify(c => c.Students.DeleteEntity(It.IsAny<Student>()), Times.Once);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_WhenDbUpdateException_ReturnsRedirectWithError()
        {
            // Arrange
            int studentId = 1;
            var student = new Student { ID = studentId, FirstMidName = "John", LastName = "Doe" };

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                It.IsAny<Expression<Func<Student, bool>>>(),
                It.IsAny<List<string>>()
            )).ReturnsAsync(student);
 

            mockRepository.Setup(repo => repo.Students.DeleteEntity(It.IsAny<Student>()));

            mockRepository.Setup(repo => repo.SaveAsync()).ThrowsAsync(new DbUpdateException());

            // Act
            var result = await controller.DeleteConfirmed(studentId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirectToActionResult.ActionName);  
            
            bool saveChangesError = (bool)(redirectToActionResult.RouteValues?["saveChangesError"] ?? false);
            Assert.True(saveChangesError, "saveChangesError should be true.");
        }

        [Fact]
        public async Task EditPost_RedirectsToIndex_WithValidModel()
        {
            // Arrange
            int studentId = 1;
            var studentViewModel = mockStudentViewModels.Find(i => i.ID == studentId);

            var student =  mockStudents.Find(i => i.ID == studentId);

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                It.IsAny<Expression<System.Func<Student, bool>>>(),
                null
            )).ReturnsAsync(student);

            mockRepository.Setup(repo => repo.Students.Update(It.IsAny<Student>()));
            mockRepository.Setup(repo => repo.SaveAsync());

            mockMapper.Setup(mapper => mapper.Map(It.IsAny<StudentViewModel>(), It.IsAny<Student>()))
                    .Callback<StudentViewModel, Student>((vm, s) => 
                    {
                        s.ID = vm.ID;
                        s.FirstMidName = vm.FirstMidName;
                        s.LastName = vm.LastName;
                    });

            // Act
            var result = await controller.EditPost(studentId, studentViewModel!);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);  

            mockRepository.Verify(c => c.Students.Update(It.IsAny<Student>()), Times.Once);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task EditPost_ReturnsViewWithModel_WithInvalidModel()
        {
            // Arrange
            int studentId = 1;
            var studentViewModel = new StudentViewModel { ID = studentId, LastName = "" };

            controller.ModelState.AddModelError("LastName", "The LastName field is required.");

            // Act
            var result = await controller.EditPost(studentId, studentViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StudentViewModel>(viewResult.Model);
            Assert.Equal(studentViewModel, model);  
        }

        [Fact]
        public async Task EditPost_ReturnsNotFound_WhenStudentNotFound()
        {
            // Arrange
            int studentId = 1;
            var studentViewModel = new StudentViewModel
            {
                ID = studentId,
                FirstMidName = "John",
                LastName = "Doe"
            };

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                It.IsAny<Expression<Func<Student, bool>>>(),
                null
                )).ReturnsAsync((Student?)null);


            // Act
            var result = await controller.EditPost(studentId, studentViewModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);  
        }

        [Fact]
        public async Task EditPost_ReturnsViewWithErrorMessage_DbUpdateException()
        {
            // Arrange
            int studentId = 1;
            var studentViewModel = new StudentViewModel
            {
                ID = studentId,
                FirstMidName = "John",
                LastName = "Doe"
            };

            var student = new Student
            {
                ID = studentId,
                FirstMidName = "John",
                LastName = "Doe"
            };

            mockRepository.Setup(repo => repo.Students.GetTAsync(
                It.IsAny<Expression<Func<Student, bool>>>(),
                null
            )).ReturnsAsync(student);

            mockRepository.Setup(repo => repo.Students.Update(It.IsAny<Student>()));
            mockRepository.Setup(repo => repo.SaveAsync()).ThrowsAsync(new DbUpdateException());

            // Act
            var result = await controller.EditPost(studentId, studentViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<StudentViewModel>(viewResult.Model);
            Assert.Equal(studentViewModel, model);  
            Assert.True(viewResult.ViewData.ModelState.ContainsKey(""));  
        }

    }
}