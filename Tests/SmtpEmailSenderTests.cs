using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmallShopSystem.Models;
using SmallShopSystem.Services.Email;
using Xunit;

namespace Tests
{
    public class SmtpEmailSenderTests
    {
        [Fact]
        public async Task SendEmailAsync_WithInvalidHost_ThrowsAndLogsError()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Host = "", // invalid
                Port = 0,
                FromEmail = "noreply@test.local",
                FromName = "Test"
            };
            var options = Options.Create(settings);
            var loggerMock = new Mock<ILogger<SmtpEmailSender>>();
            var sender = new SmtpEmailSender(options, loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => sender.SendEmailAsync("to@test.local", "subj", "body"));

            // Verify that an error was logged at least once (LogError)
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }
    }
}
