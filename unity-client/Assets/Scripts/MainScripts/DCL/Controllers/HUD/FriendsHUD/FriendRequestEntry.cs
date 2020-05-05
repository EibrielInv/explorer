using UnityEngine;
using UnityEngine.UI;

public class FriendRequestEntry : FriendsHUDListEntry
{
    [SerializeField] internal Button acceptButton;
    [SerializeField] internal Button rejectButton;
    [SerializeField] internal Button cancelButton;

    public bool isReceived { get; private set; }

    public event System.Action<FriendRequestEntry> OnAccepted;
    public event System.Action<FriendRequestEntry> OnRejected;
    public event System.Action<FriendRequestEntry> OnCancelled;

    protected override void Awake()
    {
        base.Awake();

        acceptButton.onClick.AddListener(() => OnAccepted?.Invoke(this));
        rejectButton.onClick.AddListener(() => OnRejected?.Invoke(this));
        cancelButton.onClick.AddListener(() => OnCancelled?.Invoke(this));
    }

    public void Populate(string userId, FriendEntry.Model model, bool? isReceived = null)
    {
        base.Populate(userId, model);

        if (isReceived.HasValue)
        {
            if (isReceived.Value)
                PopulateReceived();
            else
                PopulateSent();
        }
    }

    void PopulateReceived()
    {
        isReceived = true;
        cancelButton.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(true);
        rejectButton.gameObject.SetActive(true);
    }

    void PopulateSent()
    {
        isReceived = false;
        cancelButton.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
    }
}
