= FAQ

== General
How stable is Composable?::
The event store and the document database have been proven in production for a number of years. The current code has only minor changes. The service bus is new, under development, and untried in production. Of course all the components have test suites. No component has known bugs. We normally prioritize bug fixes over all feature development.

What type of infrastructure does Composable require?::
Composable uses a relational database for all storage. Currently the only supported database is Sql Server. Other than a Sql Server instance composable components have no special requirements.

[#performance]
== Performance & scalability

How well does Composable components perform?::
Very well and it will only get better. We have spent a ton of effort making sure that both performance and scalability is as high as possible without sacrificing reliability, productivity and maintainability. We have automated performance degradation tests that run continuously as we develop. We continuously look for ways to make things even faster and as we do we adjust the tests to require the new level of performance that we have achieved.

What if we need extreme querying scalability?::
Given this requirement we assume that you are willing to sacrifice some level of consistency in query results. These are then possible options:
* Maintain query models in any storage with sufficient scalablility.
* Add a layer on top of your querying API that makes use of a distributed cache.

What if we need extreme write side scalability?::
If only a small subset of your data requires this level of write scalability, consider implementing only that part using other tools. If most of your data requires that level of scalability we would not recommend using our tools.

== Reliability

Why do you enforce transactions for all updates?::
Because it is our opinion that without transactions you have to sacrifice either reliability or simplicity/maintainability and we prioritize those over scalability and performance.

Why do you enforce exactly once delivery for domain events?::
Because it is our opinion that without exactly once delivery you have to sacrifice either reliability or simplicity/maintainability and we prioritize those over scalability and performance.

Isn't exactly once delivery impossible?::
No. All arguments to that effect that we have encountered reason by disallowing transactions and/or message deduplication. Only through imposing this artificial constraint does exactly once delivery become "impossible". Exactly once  == at least once + deduplication + transactions.

