using System.Linq.Expressions;
using AutoMapper;
using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Models.ViewModels;
using ContosoUniversity.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
namespace MyWeb.Tests
{
    public class InstructorsControllerTests{
        private readonly Mock<IRepositoryService> mockRepository;
        private readonly Mock<IMapper> mockMapper;
        private readonly InstructorsController controller;
        
        public InstructorsControllerTests(){
            mockRepository = new Mock<IRepositoryService>();
            mockMapper = new Mock<IMapper>();
            controller = new InstructorsController(mockRepository.Object, mockMapper.Object);
        }
        // Data
        private List<Instructor> instructorMocks = new List<Instructor>
        {
            new Instructor { ID = 1, FirstMidName = "John", LastName = "Doe" 
            , CourseAssignments = new List<CourseAssignment>
                {
                    new CourseAssignment { Course = new Course
                        { 
                            CourseID = 1, Title = "Math 101", Enrollments = new List<Enrollment>
                            {
                                new Enrollment { Student = new Student { ID = 1, FirstMidName = "Student 1" } }
                            }
                        }
                    }
                }
            },
            new Instructor { ID = 2, FirstMidName = "Jane", LastName = "Smith" 
            , CourseAssignments = new List<CourseAssignment>
                {
                    new CourseAssignment { Course = new Course
                        { 
                            CourseID = 1, Title = "Math 101", Enrollments = new List<Enrollment>
                            {
                                new Enrollment { Student = new Student { ID = 2, FirstMidName = "Student 2" } }
                            }
                        }
                    }
                }
            }
        };

        // All Tests

        [Fact]
        public async Task Index_NoIdOrCourseId_ShouldReturnViewWithInstructors()
        {
            mockRepository.Setup(c => c.Instructors.GetAllAsync(
                null,null,
                new List<string>{ 
                    "OfficeAssignment", 
                    "CourseAssignments.Course.Enrollments.Student", 
                    "CourseAssignments.Course.Department" 
                }
            )).ReturnsAsync(instructorMocks);

            var result = await controller.Index(null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var viewData = Assert.IsType<InstructorIndexViewData>(viewResult.Model);
            Assert.Equal(2, viewData.Instructors.Count());
        }

        [Fact]
        public async Task Details_ReturnNotFound_WithNullId()
        {

            int? id = null;

            var result = await controller.Details(id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnNotFound_WithNonExistentInstructor()
        {
            int? id = 1;

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Instructor, bool>>>(),
                    It.IsAny<List<string>>()
                ))
                .ReturnsAsync((Instructor?) null); 

            var result = await controller.Details(id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnViewWithInstructorViewModel_WithValidId()
        {

            var id = 1;
            var instructor = instructorMocks.Find(i => i.ID == id)!;

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Instructor, bool>>>(),
                    It.IsAny<List<string>>()
                ))
                .ReturnsAsync(instructor);


            mockMapper.Setup(
                m => m.Map<InstructorViewModel>(It.IsAny<Instructor>()
            )).Returns((Instructor instructor) =>{
                return new InstructorViewModel(){
                    ID = instructor.ID, 
                    FirstMidName = instructor.FirstMidName, 
                    LastName  = instructor.LastName,
                    CourseAssignments  = null
                };
            });

            var result = await controller.Details(id);


            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<InstructorViewModel>(viewResult.Model);
            Assert.Equal("John", viewModel.FirstMidName);
            Assert.Equal("Doe", viewModel.LastName);
        }

        [Fact]
        public async Task Create_ReturnsViewWithModel_WhenInvalidModelState()
        {
            // Arrange
            var selectedCourses = new string[] { "1", "2" };

            var instructorViewModel = new InstructorViewModel
            {
                ID = 1,
                LastName = "John Doe"
            };

            controller.ModelState.AddModelError("Name", "Required"); 
            mockRepository.Setup(c => c.Courses.GetAllAsync(
                null,null,null
            )).ReturnsAsync(

                new List<Course>(){
                    new Course(){
                        CourseID = 1,
                        Title = "C1"
                    },
                    new Course(){
                        CourseID = 2,
                        Title = "C2"
                    },
                    new Course(){
                        CourseID = 3,
                        Title = "C3"
                    },
                }
            );
            // Act
            var result = await controller.Create(instructorViewModel, selectedCourses);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(instructorViewModel, viewResult.Model); 
            mockRepository.Verify(c => c.Instructors.AddAsync(It.IsAny<Instructor>()), Times.Never);
            mockRepository.Verify(c => c.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Create_ReturnRedirectsToIndex_ValidInstructorWithCourses()
        {
            // Arrange

            var selectedCourses = new string[] { "1", "2" };

            var instructorViewModel = new InstructorViewModel
            {
                ID = 1,
                LastName = "John Doe"
            };
            mockMapper.Setup(m => m.Map<Instructor>(It.IsAny<InstructorViewModel>()))
                .Returns((InstructorViewModel instructor) =>{
                    return new Instructor(){
                        ID = instructor.ID,
                        LastName = instructor.LastName                     
                    };
                });

            mockRepository.Setup(c => c.Instructors.AddAsync(It.IsAny<Instructor>())).Returns(Task.CompletedTask);
            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);


            // Act
            var result = await controller.Create(instructorViewModel, selectedCourses);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.Equal(2, instructorViewModel.CourseAssignments!.Count);
            Assert.Equal(1, instructorViewModel.CourseAssignments.ElementAt(0).CourseID);
            Assert.Equal(2, instructorViewModel.CourseAssignments.ElementAt(1).CourseID);

            mockRepository.Verify(c => c.Instructors.AddAsync(It.Is<Instructor>(i => i.ID == 1)), Times.Once);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }


        [Fact]
        public async Task DeleteConfirmed_ReturnRedirectToAction_WhenInstructorDoesNotExist()
        {
            // Arrange
            int instructorId = 1;

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                It.Is<Expression<Func<Instructor, bool>>>(expr => expr.Compile().Invoke(new Instructor { ID = 1 })),
                It.IsAny<List<string>>()))
                .ReturnsAsync((Instructor?)null);

            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await controller.DeleteConfirmed(instructorId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            mockRepository.Verify(c => c.Instructors.DeleteEntity(It.IsAny<Instructor>()), Times.Never);
            mockRepository.Verify(c => c.Departments.Update(It.IsAny<Department>()), Times.Never);
            mockRepository.Verify(c => c.SaveAsync(), Times.Once); 
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnRedirectToAction_DeletesInstructorAndUpdatesDepartments()
        {
            // Arrange
            int instructorId = 1;

            var instructor = instructorMocks.Find(i => i.ID == instructorId);
            // Departments liên quan
            var departments = new List<Department>
            {
                new Department { DepartmentID = 1, InstructorID = instructorId },
                new Department { DepartmentID = 2, InstructorID = instructorId }
            };

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                It.Is<Expression<Func<Instructor, bool>>>(expr => expr.Compile().Invoke(new Instructor { ID = 1 })),
                new List<string>{ "CourseAssignments"}
            )).ReturnsAsync(instructor);

            mockRepository.Setup(c => c.Departments.GetAllAsync(
                It.Is<Expression<Func<Department, bool>>>(expr => expr.Compile().Invoke(new Department { InstructorID = 1 })),
                null,null
            )).ReturnsAsync(departments);

            mockRepository.Setup(c => c.SaveAsync()).Returns(Task.CompletedTask);


            // Act
            var result = await controller.DeleteConfirmed(instructorId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            mockRepository.Verify(c => c.Instructors.DeleteEntity(instructor!), Times.Once);
            foreach (var department in departments)
            {
                Assert.Null(department.InstructorID); 
            }
            mockRepository.Verify(c => c.Departments.Update(It.IsAny<Department>()), Times.Exactly(departments.Count));
            mockRepository.Verify(c => c.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task EditPost_ReturnsNotFound_WhenInstructorNotFound()
        {
            // Arrange

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                It.Is<Expression<Func<Instructor, bool>>>(expr => expr.Compile().Invoke(new Instructor { ID = 1 })),
                It.IsAny<List<string>>()))
                .ReturnsAsync((Instructor?)null);

            // Act
            var result = await controller.EditPost(1, new string[0], new InstructorViewModel());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var instructorToUpdate = new Instructor { ID = 1 };

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                It.Is<Expression<Func<Instructor, bool>>>(expr => expr.Compile().Invoke(new Instructor { ID = 1 })),
                It.IsAny<List<string>>()))
            .ReturnsAsync(instructorToUpdate);

            mockRepository.Setup(c => c.Courses.GetAllAsync(
                null,null,null
            )).ReturnsAsync(

                new List<Course>(){
                    new Course(){
                        CourseID = 1,
                        Title = "C1"
                    },
                    new Course(){
                        CourseID = 2,
                        Title = "C2"
                    },
                    new Course(){
                        CourseID = 3,
                        Title = "C3"
                    },
                }
            );

            mockMapper.Setup(
                m => m.Map<InstructorViewModel>(It.IsAny<Instructor>()
            )).Returns(new InstructorViewModel(){
                CourseAssignments = new List<CourseAssignmentViewModel>()
            });


            controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await controller.EditPost(1, new string[0], new InstructorViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(instructorToUpdate, viewResult.Model);


        }


        [Fact]
        public async Task EditPost_ReturnRedirectToAction_UpdatesCourseAssignmentsAndInstructor()
        {
            // Arrange
            var instructorToUpdate = new Instructor { 
                ID = 1, 
                FirstMidName = "To Update", 
                OfficeAssignment = new(),
                CourseAssignments  = new List<CourseAssignment>{
                    new CourseAssignment(){
                        CourseID = 3
                    }
                }
            };

            mockRepository.Setup(c => c.Instructors.GetTAsync(
                It.Is<Expression<Func<Instructor, bool>>>(expr => expr.Compile().Invoke(new Instructor { ID = 1 })),
                It.IsAny<List<string>>()))
                .ReturnsAsync(instructorToUpdate);

            mockRepository.Setup(c => c.OfficeAssignments.DeleteEntity(instructorToUpdate.OfficeAssignment));

            mockRepository.Setup(c => c.CourseAssignments.DeleteEntity(It.IsAny<CourseAssignment>()));

            mockRepository.Setup(c => c.CourseAssignments.AddAsync(It.IsAny<CourseAssignment>()));

            mockRepository.Setup(c => c.Courses.GetAllAsync(
                null,null,null
            )).ReturnsAsync(

                new List<Course>(){
                    new Course(){
                        CourseID = 1,
                        Title = "C1"
                    },
                    new Course(){
                        CourseID = 2,
                        Title = "C2"
                    },
                    new Course(){
                        CourseID = 3,
                        Title = "C3"
                    },
                }
            );

            var instructorViewModel = new InstructorViewModel { ID = 1, FirstMidName = "Updated VM", OfficeAssignmentLocation = ""};
            var selectedCourses = new[] { "1", "2" };

            mockMapper.Setup(m => m.Map(instructorViewModel, instructorToUpdate));


            // Act
            var result = await controller.EditPost(1, selectedCourses, instructorViewModel);

            // Assert
            mockRepository.Verify(c => c.Instructors.UpdateEntity(instructorToUpdate), Times.Once);

            mockRepository.Verify(c => c.CourseAssignments.DeleteEntity(It.IsAny<CourseAssignment>()), Times.Once);

            mockRepository.Verify(c => c.CourseAssignments.AddAsync(It.IsAny<CourseAssignment>()), Times.Exactly(2));

            mockRepository.Verify(c => c.SaveAsync(), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

    }
}