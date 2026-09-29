using FluentAssertions;
using HotelListing.Api.Controllers;
using HotelListing.Api.Conventions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;
using Xunit;

namespace HotelListing.Api.Tests.Conventions;

public class ApiControllerBaseConventionTests
{
    private readonly ApiControllerBaseConvention _sut = new();

    [Fact]
    public void Apply_ForHotelsController_GetHotels_AddsExpectedStatusCodes()
    {
        // Arrange
        var controllerType = typeof(HotelsController);
        var methodInfo = controllerType.GetMethod(nameof(HotelsController.GetHotels))!;

        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), controllerType.GetCustomAttributes(true).ToList());
        var actionModel = new ActionModel(methodInfo, methodInfo.GetCustomAttributes(true).ToList())
        {
            Controller = controllerModel
        };

        // Act
        _sut.Apply(actionModel);

        // Assert
        var statusCodes = actionModel.Filters
            .OfType<ProducesResponseTypeAttribute>()
            .Select(f => f.StatusCode)
            .ToList();

        statusCodes.Should().Contain(StatusCodes.Status200OK);
        statusCodes.Should().Contain(StatusCodes.Status500InternalServerError);
        statusCodes.Should().Contain(StatusCodes.Status401Unauthorized);
        statusCodes.Should().Contain(StatusCodes.Status403Forbidden);
        statusCodes.Should().NotContain(StatusCodes.Status404NotFound);
    }
}
