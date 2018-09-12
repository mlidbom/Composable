== Primary architectural choices

We require you to learn a "new" modelling paradigm, semantic events::
We do not believe that we would be able to build highly complicated software using a language that did not support a flexible modelling paradigm, such as object oriented programming, without great loss of productivity, simplicity and safety. We believe that such standardized modelling paradigms enable us to successfully manage far greater levels of complexity than we could without them.
+
This is what we aim to achieve by ubiquitously leveraging the semantic event modeling paradigm throughout our tools. Using the existing interface support in C# we enable you to model a highly complicated domain in terms of which events can occur and how the meanings of these events relate to each other, the aggregate root to which they belong, and to other abstractions in the domain. The same modelling paradigm is utilized at all levels of design. From the smallest component or entity nested in an aggregate to designing and implementing the highest level integrations in an ecosystem of integrated systems.
+
Our experience is that this new modelling paradigm, this new view of your domain and ecosystem of domains, is highly beneficial to managing and scaling complexity of the domains and integrations between them. We also find it highly beneficial in communicating with with domain experts. It has given us an expressive way of viewing a system in terms of what can happen, without getting bogged down in the details of how.
+
On the technical side it enables us to dramatically cut down on manual code and manual configuration. It enables us to build complicated aggregates, read models, and message based integrations with simple, SOLID, expressive code. All with zero manual routing code or configuration to take into consideration. You can use the same powerful mental model to understand how everything will work at all levels instead of having to design and remember countless custom routes within aggregates and within the bus.
+
Our experience is that this as a whole dramatically reduces the total mental burden of understanding a large system or ecosystem of systems. Our experience is that it allows us to scale to far greater complexity of a domain and exposed features without development slowing or virtually stalling due to the runaway mental burden of an implementation that does not leverage a powerful modelling paradigm.

We enforce the use of transactions for all domain data updates::

We enforce the use of exactly once delivery for domain events and commands::