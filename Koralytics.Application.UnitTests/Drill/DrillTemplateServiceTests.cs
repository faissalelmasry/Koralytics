using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Drill;
using Koralytics.Application.Services.Drill.DrillTemplate;
using Koralytics.Domain.Entities.Drill;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Koralytics.Application.UnitTests.Drill
{
    public class DrillTemplateServiceTests
    {
        // 1. Declare our fakes (Mocks)
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;

        // 2. Declare the actual service we are testing
        private readonly DrillTemplateService _service;

        public DrillTemplateServiceTests()
        {
            // Initialize the mocks
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            // Inject the fakes into the real service
            _service = new DrillTemplateService(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        // ================================================================
        // CreateTemplateAsync Tests
        // ================================================================

        [Fact]
        public async Task CreateTemplateAsync_CategoryDoesNotExist_ThrowsKeyNotFoundException()
        {
            // --- ARRANGE ---
            var dto = new CreateDrillTemplateDto { CategoryId = 99 }; // Fake ID

            // Create a fake repository for DrillCategory
            var categoryRepoMock = new Mock<IRepository<DrillCategory>>();

            // Tell the fake repo: When someone calls ExistsAsync, always return FALSE
            categoryRepoMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<DrillCategory, bool>>>()))
                .ReturnsAsync(false);

            // Connect the fake repo to the fake UnitOfWork
            _unitOfWorkMock
                .Setup(u => u.Repository<DrillCategory>())
                .Returns(categoryRepoMock.Object);

            // --- ACT & ASSERT ---
            // Verify that calling the method throws the exact exception we expect
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateTemplateAsync(dto, currentUserId: 1, currentUserRole: "Coach", currentUserAcademyId: 5));
        }

        [Fact]
        public async Task CreateTemplateAsync_ValidData_CreatesAndReturnsTemplate()
        {
            // --- ARRANGE ---
            var dto = new CreateDrillTemplateDto { CategoryId = 1, Name = "Passing Drill" };
            var drillTemplateEntity = new DrillTemplate { Id = 10, Name = "Passing Drill" };
            var returnedDto = new DrillTemplateDto { Id = 10, Name = "Passing Drill" };

            // 1. Mock Category Exists check (Return TRUE)
            var categoryRepoMock = new Mock<IRepository<DrillCategory>>();
            categoryRepoMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<DrillCategory, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<DrillCategory>()).Returns(categoryRepoMock.Object);

            // 2. Mock Template Repository (For adding the entity)
            var templateRepoMock = new Mock<IRepository<DrillTemplate>>();
            templateRepoMock
                .Setup(r => r.AddAsync(It.IsAny<DrillTemplate>()))
                .Returns(Task.CompletedTask); // Simulate a successful save
            _unitOfWorkMock.Setup(u => u.Repository<DrillTemplate>()).Returns(templateRepoMock.Object);

            // 3. Mock AutoMapper
            _mapperMock.Setup(m => m.Map<DrillTemplate>(dto)).Returns(drillTemplateEntity);
            _mapperMock.Setup(m => m.Map<DrillTemplateDto>(drillTemplateEntity)).Returns(returnedDto);

            // --- ACT ---
            var result = await _service.CreateTemplateAsync(dto, currentUserId: 1, currentUserRole: "SystemAdmin", currentUserAcademyId: null);

            // --- ASSERT ---
            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
            Assert.Equal("Passing Drill", result.Name);

            // Verify that SaveChangesAsync was actually called exactly once
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once());
        }
    }
}