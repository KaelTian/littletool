using EasyMessageHub;
using Shouldly;

namespace Tools.Tests.Unit
{
    [TestFixture]
    internal class MessageHubTests
    {
        [Test]
        public void When_subscribing_handlers_with_one_throwing_exception()
        {
            var hub = new MessageHub();

            var queue = new List<string>();
            var totalMsgs = new List<string>();
            var errors = new List<KeyValuePair<Guid, Exception>>();

            hub.RegisterGlobalHandler((type, msg) =>
            {
                type.ShouldBe(typeof(string));
                msg.ShouldBeOfType<string>();
                totalMsgs.Add((string)msg);
            });

            hub.RegisterGlobalErrorHandler(
                (token, e) => errors.Add(new KeyValuePair<Guid, Exception>(token, e)));

            Action<string> subscriberOne = msg => queue.Add("Sub1-" + msg);
            Action<string> subscriberTwo = msg => { throw new InvalidOperationException("Ooops-" + msg); };
            Action<string> subscriberThree = msg => queue.Add("Sub3-" + msg);

            hub.Subscribe(subscriberOne);
            var subTwoToken = hub.Subscribe(subscriberTwo);
            hub.Subscribe(subscriberThree);
            hub.Publish("A");
            //hub.Publish<object>("撒库拉");
            //hub.Publish(133);

            Action<string> subscriberFour = msg => { throw new InvalidCastException("Aaargh-" + msg); };
            var subFourToken = hub.Subscribe(subscriberFour);

            hub.Publish("B");

            queue.Count.ShouldBe(4);
            queue[0].ShouldBe("Sub1-A");
            queue[1].ShouldBe("Sub3-A");
            queue[2].ShouldBe("Sub1-B");
            queue[3].ShouldBe("Sub3-B");

            totalMsgs.Count.ShouldBe(2);
            totalMsgs.ShouldContain(msg => msg == "A");
            totalMsgs.ShouldContain(msg => msg == "B");

            errors.Count.ShouldBe(3);
            errors.ShouldContain(err =>
                err.Value.GetType() == typeof(InvalidOperationException)
                && err.Value.Message == "Ooops-A"
                && err.Key == subTwoToken);

            errors.ShouldContain(err =>
                err.Value.GetType() == typeof(InvalidOperationException)
                && err.Value.Message == "Ooops-B"
                && err.Key == subTwoToken);

            errors.ShouldContain(err =>
                err.Value.GetType() == typeof(InvalidCastException)
                && err.Value.Message == "Aaargh-B"
                && err.Key == subFourToken);
        }
    }
}
