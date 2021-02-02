# Unity DOTS Multiplayer XR Sample
Sample project with stable Unity, Entities, Physics, NetCode, UI Builder, UI Toolkit and Multiplayer

## Why create this sample project?


At Moetsi we work on Reality Modeling technology . A lot of our use-cases require both desktop and "live" (AR) client networking. We create and stream updated Reality Models in real-time to connected clients, which is data-intensive.

We have chosen DOTS and DOTS NetCode for our interaction layer because DOTS is able to handle heavy data processing without draining batteries and DOTS NetCode uses an authoritative model that works best for Reality Modeling.

Unity DOTS is still in the beginning of its development and has been going through some great, but breaking iterations. The Moetsi team has kept up with its evolution and how to connected different pieces of Unity's technology together. A lot of this information we could not find online which means there are probably a lot of developers going through the same (painful) process. Connecting GameObject Unity and ECS Unity (hybrid development) seems to be especially missing for things like UI Toolkit and UI Builder.

We are providing a tutorial and sample project for how to create a multiplayer real-time XR experience using Unity's DOTS. Anyone that has been interested in trying out DOTS but are concerned about stability or package interoperability can use this tutorial knowing that "it will work".
We will build a project that can deploy to both desktop and ARKit platforms. Desktop players will be able to navigate using WASDF keys and AR players will be able to navigate using their device movement.

## End Result

Link to iOS
Link to Mac store
Link to Windows
