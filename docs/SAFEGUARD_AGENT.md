---
layout: default
title: "Implementing a Safeguard with an Agent"
---
![task-manager-with-gardian](task-manager-with-gardian.jpg)

- [Task Manager with an Assistant]({{ '/README' | relative_url }})


Securing a software system is important, and fortunately, there are traditional methods and patterns for doing it. You get authenticated (you know — using blood, a piece of skin, aye, retina scan, fingerprint, or simply a password), then you get authorized — what you are allowed to do.

Making sure a naïve, helpful assistant doesn’t do on behalf of the user what the user is not allowed to do? That’s a whole other matter.

Here’s what I learned.

## Why a Guardian Agent?

For more complex systems, it’s best to use dedicated tools (like [like NVIDIA NeMo Guardrails](https://docs.nvidia.com/nemo-guardrails/index.html)). In my case, I decided to put a police agent — a guardian — at the front door, with the sole purpose of protecting the care-free worker.

![worflow-diagram](workflow-diagram5.jpg)

For the test, I targeted the rule of **not deleting a task without asking for confirmation**. Without a guardian, and after some “convincing,” I finally got the assistant to ignore this rule:

![delete-a-task-no-confirmation](delete-a-task-no-confirmation.jpg)

Microsoft’s built-in content filtering helps:

![microsoft-safeguard](microsoft-safeguard.jpg)

However, for system-specific rules, your own guardian is required:

![delete-a-task-no-confirmation-guardian](delete-a-task-no-confirmation-guardian.jpg)

When a task is created, the assistant is supposed to ask for title, description, and due date: 
![add-a-task-rule-guardian](add-a-task-rule-gardian.jpg)

There’s a tool for the assistant to know today’s date, which is important if you want to create a task due today: 
![change-today-date-guardian](change-today-date-guardian.jpg)

Prompting is key. Initially, my guardian overreacted — marking even safe actions as threats. A more detailed briefing helped it distinguish **actual danger** from normal operations.

---

## Optional “Back-Door”

For testing, I implemented a controlled back-door called **Miranda**. Including “Miranda” in the prompt bypasses the guardian. Not recommended for production!

![miranda](miranda.jpg)

---

Feel free to check out the [Pull Request](https://github.com/kiril-vassilev/task-manager-with-assistant/pull/3)
