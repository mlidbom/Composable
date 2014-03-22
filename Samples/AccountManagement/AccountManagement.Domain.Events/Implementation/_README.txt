Concrete events should be used only by the domain and tests and only when creating an event.
If you ever see a line of code that uses an event and does not start with "new" it is almost certainly WRONG.

See: Domain Events on the wiki