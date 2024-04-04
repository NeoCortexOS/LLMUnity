using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine.UI;
using System.Collections;

namespace LLMUnitySamples
{
    public class ChatBot : MonoBehaviour
    {
        public Transform chatContainer;
        public Color playerColor = new Color32(81, 164, 81, 255);
        public Color aiColor = new Color32(29, 29, 73, 255);
        public Color fontColor = Color.white;
        public Font font;
        public int fontSize = 16;
        public int bubbleWidth = 600;
        public LLM llm;
        public float textPadding = 10f;
        public float bubbleSpacing = 10f;
        public Sprite sprite;

        private InputBubble inputBubble;
        private List<Bubble> chatBubbles = new List<Bubble>();
        private bool blockInput = true;
        private BubbleUI playerUI, aiUI, inputUI;
        private bool warmUpDone = false;
        private int lastBubbleOutsideFOV = -1;
        public GameObject LoadingScreen;
        public Text LoadingText;

        void Start()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerUI = new BubbleUI
            {
                sprite = sprite,
                font = font,
                fontSize = fontSize,
                fontColor = fontColor,
                bubbleColor = playerColor,
                bottomPosition = 0,
                leftPosition = 0,
                textPadding = textPadding,
                bubbleOffset = bubbleSpacing,
                bubbleWidth = bubbleWidth,
                bubbleHeight = -1
            };
            aiUI = playerUI;
            aiUI.bubbleColor = aiColor;
            aiUI.leftPosition = 1;
            inputUI = playerUI;
            inputUI.bubbleHeight = 180;

            StartCoroutine(Loading());
        }

        IEnumerator Loading()
        {
            LoadingText.text = "Starting server...";
            LoadingScreen.gameObject.SetActive(true);
            // wait until server is up
            while (!llm.serverStarted)
            {
                yield return null;
            }
            //warm-up the model
            LoadingText.text = "Warming-up the model...";
            _ = llm.Warmup(WarmUpCallback);
        }

        void onInputFieldSubmit(string newText)
        {
            inputBubble.ActivateInputField();
            if (blockInput || newText.Trim() == "" || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                StartCoroutine(BlockInteraction());
                return;
            }
            blockInput = true;
            // replace vertical_tab
            string message = inputBubble.GetText().Replace("\v", "\n");

            Bubble playerBubble = new Bubble(chatContainer, playerUI, "PlayerBubble", message);
            Bubble aiBubble = new Bubble(chatContainer, aiUI, "AIBubble", "...");
            chatBubbles.Add(playerBubble);
            chatBubbles.Add(aiBubble);
            playerBubble.OnResize(UpdateBubblePositions);
            aiBubble.OnResize(UpdateBubblePositions);
            inputBubble.SetText("");

            // call LLM
            Task chatTask = llm.Chat(message, aiBubble.SetText, AllowInput);
        }

        public void WarmUpCallback()
        {
            LoadingScreen.gameObject.SetActive(false);
            warmUpDone = true;
            CreateInputBubble();
            AllowInput();
        }

        public void CreateInputBubble()
        {
            inputBubble = new InputBubble(chatContainer, inputUI, "InputBubble", "Loading...", 4);
            inputBubble.AddSubmitListener(onInputFieldSubmit);
            inputBubble.AddValueChangedListener(onValueChanged);
            inputBubble.SetPlaceHolderText("Message me");
        }

        public void AllowInput()
        {
            blockInput = false;
            inputBubble.ReActivateInputField();
        }

        public void CancelRequests()
        {
            llm.CancelRequests();
            AllowInput();
        }

        IEnumerator BlockInteraction()
        {
            // prevent from change until next frame
            inputBubble.setInteractable(false);
            yield return null;
            inputBubble.setInteractable(true);
            // change the caret position to the end of the text
            inputBubble.MoveTextEnd();
        }

        void onValueChanged(string newText)
        {
            // Get rid of newline character added when we press enter
            if (Input.GetKey(KeyCode.Return))
            {
                if (inputBubble.GetText().Trim() == "")
                    inputBubble.SetText("");
            }
        }

        public void UpdateBubblePositions()
        {
            float y = inputBubble.GetSize().y + inputBubble.GetRectTransform().offsetMin.y + bubbleSpacing;
            float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;
            for (int i = chatBubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = chatBubbles[i];
                RectTransform childRect = bubble.GetRectTransform();
                childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x, y);

                // last bubble outside the container
                if (y > containerHeight && lastBubbleOutsideFOV == -1)
                {
                    lastBubbleOutsideFOV = i;
                }
                y += bubble.GetSize().y + bubbleSpacing;
            }
        }

        void Update()
        {
            if (inputBubble != null && !inputBubble.inputFocused() && warmUpDone)
            {
                inputBubble.ActivateInputField();
                StartCoroutine(BlockInteraction());
            }
            if (lastBubbleOutsideFOV != -1)
            {
                // destroy bubbles outside the container
                for (int i = 0; i <= lastBubbleOutsideFOV; i++)
                {
                    chatBubbles[i].Destroy();
                }
                chatBubbles.RemoveRange(0, lastBubbleOutsideFOV + 1);
                lastBubbleOutsideFOV = -1;
            }
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }
    }
}
