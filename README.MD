This creates a function app that automatically deletes resources with certain tags.

The function app will get **Contributor** rights to the subscription, so USE AT YOUR OWN RISK. This is just meant to be a proof-of-concept. Production applications will need additional guardrails, such as limiting the function app scope, running it in a private network, etc.