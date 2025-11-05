---
layout: default
title: "Implementing a Safeguard with an Agent"
---
![task-manager-with-gardian](task-manager-with-gardian.jpg)

- [Task Manager with an Assistant]({{ '/README' | relative_url }})


Securing a software system is important, and fortunately, there are traditional methods and patterns for doing it. You get authenticated (you know — using blood, a piece of skin, aye, retina scan, fingerprint, or simply a password), then you get authorized — what you are allowed to do.

Making sure a naïve, helpful assistant doesn’t do on behalf of the user what the user is not allowed to do? That’s a whole other matter.

## Security by Design

First, security by design: what would be the threats that we should be prepared for?

Arguably, accessing somebody else’s tasks is the worst break in security. (I still have to implement it, so it’s a problem for the future, but still…) The best approach is **not giving the assistant the means to access somebody else’s data at all**.

The current user (with all their authorized rights) is in the system’s current context, so all the tools that work on behalf of the user will only access the currently logged-in user’s allowed data. If you try to force the assistant to get more — it wouldn’t even know what you’re talking about.

**What is left to mess with, is the original prompt**. A user might try to force the assistant to reveal its prompt, modify it, or change the rules for using the system tools.

For more complex systems, it’s best to use dedicated tools (like [like NVIDIA NeMo Guardrails](https://docs.nvidia.com/nemo-guardrails/index.html)). In my case, I decided to put a police agent — a guardian — at the front door, with the sole purpose of protecting the care-free worker.

![worflow-diagram](workflow-diagram5.jpg)

## Testing the Guardian

For the test, I targeted the rule of **not deleting a task without asking for confirmation**. Without a guardian, and after some “convincing”, I finally got the assistant to ignore this rule:

![delete-a-task-no-confirmation](delete-a-task-no-confirmation.jpg)

When I put everything together and started experimenting with the guardian, I was surprised to find out Microsoft has built-in [content filtering](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/concepts/content-filter):

![microsoft-safeguard](microsoft-safeguard.jpg)

Of course, this is a good thing, but I was a bit disappointed to see my bodyguard had been “briefed” already. I guess this is one of the perks of using LLMs instead of your own trained SLM.

Preventing the system’s own specific rules was a job for the Task Manager’s own guardian, and it took it very seriously — deletion confirmation rule, or any other rule:

![delete-a-task-no-confirmation-guardian](delete-a-task-no-confirmation-guardian.jpg)

When a task is created, the assistant is supposed to ask for *title*, *description*, and *duedate*: 
![add-a-task-rule-guardian](add-a-task-rule-gardian.jpg)

There’s a tool for the assistant to know today’s date, which is important if you want to create a task due today: 
![change-today-date-guardian](change-today-date-guardian.jpg)

---

## Prompting the Guardian

It all boils down to prompting.

At first, I had basic instructions for my guardian — sort of: *Serve and Protect.* It didn’t work well because it saw danger almost everywhere. For example, deleting a task, or even marking it as complete, was considered a threat.

So, I had to brief it and give it a bit more context.

I don’t think I’ve covered all potential threats, and if the system were to be anywhere near production, it would need a **lot more solid testing and tweaking**.

---

## The “Back-Door”

I also did something else, which I wouldn’t recommend for production — implemented a “back-door”. Let’s call it **Miranda**.

If you want to bypass the guardian, you just have to use Miranda in the question.

![miranda](miranda.jpg)

---

Feel free to check out the [Pull Request](https://github.com/kiril-vassilev/task-manager-with-assistant/pull/3) and share any comments or questions. (Since my wife’s name is not Miranda, I already know what her first question is going to be.)

![do-you-know-miranda](red-heat-danko.gif)
