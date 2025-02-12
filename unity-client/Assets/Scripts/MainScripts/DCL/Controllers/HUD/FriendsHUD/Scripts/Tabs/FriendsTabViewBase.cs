using DCL.Helpers;
using DCL.Configuration;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DCL;

public class FriendsTabViewBase : MonoBehaviour, IPointerDownHandler
{
    [System.Serializable]
    public class LastFriendTimestampModel
    {
        public string userId;
        public ulong lastMessageTimestamp;
    }

    [System.Serializable]
    public class EntryList
    {
        public string toggleTextPrefix;
        public GameObject toggleButton;
        public TextMeshProUGUI toggleText;
        public Transform container;
        private Dictionary<string, FriendEntryBase> entries = new Dictionary<string, FriendEntryBase>();

        // This list store each friendId with the greatest timestamp from his related messages
        private List<LastFriendTimestampModel> latestTimestampsOrdered = new List<LastFriendTimestampModel>();

        public int Count()
        {
            return entries.Count;
        }

        public void Add(string userId, FriendEntryBase entry)
        {
            if (entries.ContainsKey(userId))
                return;

            entries.Add(userId, entry);

            entry.transform.SetParent(container, false);
            entry.transform.localScale = Vector3.one;

            UpdateToggle();
            ReorderingFriendEntries();
        }

        public FriendEntryBase Remove(string userId)
        {
            if (!entries.ContainsKey(userId))
                return null;

            var entry = entries[userId];

            entries.Remove(userId);

            UpdateToggle();

            return entry;
        }

        void UpdateToggle()
        {
            toggleText.text = $"{toggleTextPrefix} ({Count()})";
            toggleButton.SetActive(Count() != 0);
        }

        public void AddOrUpdateLastTimestamp(LastFriendTimestampModel timestamp, bool reorderFriendEntries = true)
        {
            if (timestamp == null)
                return;

            LastFriendTimestampModel existingTimestamp = latestTimestampsOrdered.FirstOrDefault(t => t.userId == timestamp.userId);
            if (existingTimestamp == null)
            {
                latestTimestampsOrdered.Add(timestamp);
            }
            else if (timestamp.lastMessageTimestamp > existingTimestamp.lastMessageTimestamp)
            {
                existingTimestamp.lastMessageTimestamp = timestamp.lastMessageTimestamp;
            }

            if (reorderFriendEntries)
            {
                latestTimestampsOrdered = latestTimestampsOrdered.OrderByDescending(f => f.lastMessageTimestamp).ToList();
                ReorderingFriendEntries();
            }
        }

        public LastFriendTimestampModel RemoveLastTimestamp(string userId)
        {
            LastFriendTimestampModel timestampToRemove = latestTimestampsOrdered.FirstOrDefault(t => t.userId == userId);
            if (timestampToRemove == null)
                return null;

            latestTimestampsOrdered.Remove(timestampToRemove);

            return timestampToRemove;
        }

        private void ReorderingFriendEntries()
        {
            foreach (var item in latestTimestampsOrdered)
            {
                if (entries.ContainsKey(item.userId))
                    entries[item.userId].transform.SetAsLastSibling();
            }
        }
    }

    private const string FRIEND_ENTRIES_POOL_NAME = "FriendEntriesPool_";
    [SerializeField] protected GameObject entryPrefab;
    [SerializeField] protected GameObject emptyListImage;

    protected RectTransform rectTransform;
    protected FriendsHUDView owner;

    public UserContextMenu contextMenuPanel;
    public FriendsHUD_DialogBox confirmationDialog;

    protected Dictionary<string, FriendEntryBase> entries = new Dictionary<string, FriendEntryBase>();
    protected Dictionary<string, PoolableObject> instantiatedFriendEntries = new Dictionary<string, PoolableObject>();
    protected Pool friendEntriesPool;

    internal List<FriendEntryBase> GetAllEntries()
    {
        return entries.Values.ToList();
    }

    internal FriendEntryBase GetEntry(string userId)
    {
        if (!entries.ContainsKey(userId))
            return null;

        return entries[userId];
    }

    protected virtual void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        rectTransform.ForceUpdateLayout();
    }

    protected virtual void OnDisable()
    {
        confirmationDialog.Hide();
        contextMenuPanel.Hide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerPressRaycast.gameObject == null || eventData.pointerPressRaycast.gameObject.layer != PhysicsLayers.friendsHUDPlayerMenu)
            contextMenuPanel.Hide();
    }

    public virtual void Initialize(FriendsHUDView owner, int preinstantiatedEntries)
    {
        this.owner = owner;

        friendEntriesPool = PoolManager.i.GetPool(FRIEND_ENTRIES_POOL_NAME + this.name + this.GetInstanceID());
        if (friendEntriesPool == null)
        {
            friendEntriesPool = PoolManager.i.AddPool(
                FRIEND_ENTRIES_POOL_NAME + this.name + this.GetInstanceID(),
                Instantiate(entryPrefab),
                maxPrewarmCount: preinstantiatedEntries,
                isPersistent: true);
            friendEntriesPool.ForcePrewarm();
        }

        rectTransform = transform as RectTransform;

        contextMenuPanel.OnBlock += OnPressBlockButton;
        contextMenuPanel.OnDelete += OnPressDeleteButton;
        contextMenuPanel.OnPassport += OnPressPassportButton;
        contextMenuPanel.OnReport += OnPressReportButton;
    }

    public virtual void OnDestroy()
    {
        contextMenuPanel.OnBlock -= OnPressBlockButton;
        contextMenuPanel.OnDelete -= OnPressDeleteButton;
        contextMenuPanel.OnPassport -= OnPressPassportButton;
        contextMenuPanel.OnReport -= OnPressReportButton;
    }

    protected virtual void OnPressReportButton(string userId)
    {
    }

    protected virtual void OnPressPassportButton(string userId)
    {
    }

    protected virtual void OnPressDeleteButton(string userId)
    {
    }

    protected virtual void OnPressBlockButton(string userId, bool blockUser)
    {
        FriendEntryBase friendEntryToBlock = GetEntry(userId);
        if (friendEntryToBlock != null)
        {
            friendEntryToBlock.model.blocked = blockUser;
            friendEntryToBlock.Populate(friendEntryToBlock.model);
        }
    }

    public virtual void CreateOrUpdateEntry(string userId, FriendEntryBase.Model model)
    {
        CreateEntry(userId);
        UpdateEntry(userId, model);
    }

    public virtual bool CreateEntry(string userId)
    {
        if (entries.ContainsKey(userId)) return false;

        PoolableObject newFriendEntry = friendEntriesPool.Get();
        instantiatedFriendEntries.Add(userId, newFriendEntry);
        var entry = newFriendEntry.gameObject.GetComponent<FriendEntryBase>();
        entries.Add(userId, entry);

        entry.OnMenuToggle += (x) =>
        {
            bool isBlocked = UserProfile.GetOwnUserProfile().blocked.Contains(userId);
            contextMenuPanel.Initialize(userId, string.Empty, isBlocked);
            contextMenuPanel.transform.position = entry.menuPositionReference.position;
            contextMenuPanel.Show();
        };

        UpdateEmptyListObjects();

        return true;
    }

    public virtual bool UpdateEntry(string userId, FriendEntryBase.Model model)
    {
        if (!entries.ContainsKey(userId)) return false;

        var entry = entries[userId];

        entry.Populate(model);
        entry.userId = userId;

        rectTransform.ForceUpdateLayout();
        return true;
    }

    public virtual bool RemoveEntry(string userId)
    {
        if (!entries.ContainsKey(userId)) return false;

        if (instantiatedFriendEntries.TryGetValue(userId, out PoolableObject go))
        {
            friendEntriesPool.Release(go);
            instantiatedFriendEntries.Remove(userId);
        }
        entries.Remove(userId);

        UpdateEmptyListObjects();

        rectTransform.ForceUpdateLayout();
        return true;
    }

    protected virtual void UpdateEmptyListObjects()
    {
        emptyListImage.SetActive(entries.Count == 0);
    }
}
