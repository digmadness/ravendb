namespace RavenFS.Tests.Synchronization
{
	using System;
	using System.Collections.Specialized;
	using System.Reactive.Linq;
	using System.Reactive.Threading.Tasks;
	using IO;
	using Xunit;

	public class ConflictNotificationTests : MultiHostTestBase
	{
		[Fact(Skip = "When running the build script from command line notification tests cause the crash")]
		public void NotificationIsReceivedWhenConflictIsDetected()
		{
			var destinationClient = NewClient(0);
            var sourceClient = NewClient(1);

			var sourceContent = new RandomlyModifiedStream(new RandomStream(1), 0.01);
			var destinationContent = new RandomlyModifiedStream(sourceContent, 0.01);

			var sourceMetadata = new NameValueCollection
                               {
                                   {"SomeTest-metadata", "some-value"}
                               };

            var destinationMetadata = new NameValueCollection
                               {
                                   {"SomeTest-metadata", "should-be-overwritten"}
                               };

            destinationClient.UploadAsync("abc.txt", destinationMetadata, destinationContent).Wait();
            sourceClient.UploadAsync("abc.txt", sourceMetadata, sourceContent).Wait();

			destinationClient.Notifications.Connect().Wait();

			var notificationTask = destinationClient.Notifications.ConflictDetections().Timeout(TimeSpan.FromSeconds(5)).Take(1).ToTask();

        	sourceClient.Synchronization.StartSynchronizationToAsync("abc.txt", destinationClient.ServerUrl).Wait();
			
			var conflictDetected = notificationTask.Result;

			Assert.Equal("abc.txt", conflictDetected.FileName);
			Assert.Equal(destinationClient.ServerUrl, conflictDetected.ServerUrl);
		}
	}
}