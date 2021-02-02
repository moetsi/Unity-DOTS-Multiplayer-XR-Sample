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

![Navigating in Editor](https://gblobscdn.gitbook.com/assets%2F-MPeyID8jdArWRKTskW0%2F-MR6m1FcHAUnkjukTJ7w%2F-MR6m6LwmsEvtJkIiXsS%2FUI%20-%20NavigatingScenes%20-%20Final%20Overview.gif)

![AR player getting shot down by desktop player](https://gblobscdn.gitbook.com/assets%2F-MPeyID8jdArWRKTskW0%2F-MSQkTQw9V8jcjeUZLbU%2F-MSQk_ChKcD34qcuPQvM%2FARFoundation%20-%20Updating%20UI%20-%20Getting%20shot%20down%20by%20desktop.gif)

![Desktop player shooting down AR palyer](https://gblobscdn.gitbook.com/assets%2F-MPeyID8jdArWRKTskW0%2F-MSQkTQw9V8jcjeUZLbU%2F-MSQkWsGeth5AFZOJPkL%2FARFoundation%20-%20Updating%20UI%20-%20Shooting%20down%20AR%20palyer.gif?alt=media&token=2a77b60e-b584-46fb-bcdc-34295475eec6)
