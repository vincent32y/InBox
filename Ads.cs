using FrameWork;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Advertisements;
using XLua;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Advertisements;
#endif

/// <summary>
/// 奖励广告
/// </summary>
public class Ads : Singleton<Ads>
{
	public static Ads theInstance
	{
		get { return Instance; }
	}

	private Action<int> _onRewardAds;
	private Action<bool> _onAvailability;
	//插屏广告回调
	private Action<int> _onIntersAd;
	
	private float _timeScaleCache = 1f;

	private enum Type
	{
		ADS_RESULT_NOREADY = 1,
		ADS_RESULT_FAILED = 2,
		ADS_RESULT_SUCCESS = 3,
		ADS_RESULT_SKIP = 4,
		ADS_RESULT_FINISHT = 5,
		ADS_RESULT_SHOWING = 6
	}

	public void OnAvailabilityCallBack(Action<bool> onAvailability,  Action<int> onRewardAds)
	{
		this._onAvailability = onAvailability;
		this._onRewardAds = onRewardAds;
	}

	public void OnIntersAdCallback(Action<int> onIntersAdCallback)
	{
		_onIntersAd = onIntersAdCallback;
	}
	
	public void Init(string appKey)
	{
		EzDebug.Log("unity-script: MyAppStart Start called");

		IronSource.Agent.shouldTrackNetworkState(true);
#if UNITY_EDITOR
		_onAvailability(true);
#endif

#if JX_DEBUG
		EzDebug.Log("unity-script: IronSource.Agent.validateIntegration");
		IronSource.Agent.validateIntegration();
#endif
		EzDebug.Log("unity-script: unity version" + IronSource.unityVersion());

		EzDebug.Log("unity-script: IronSource.Agent.init");
		IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);
		InitRewardAdsEvent();
		InitIntersAdEvent();
		IronSource.Agent.loadInterstitial();
	}
	
	public void Release()
	{
		IronSourceEvents.onRewardedVideoAdOpenedEvent -= RewardedVideoAdOpenedEvent;
		IronSourceEvents.onRewardedVideoAdClosedEvent -= RewardedVideoAdClosedEvent;
		IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= RewardedVideoAvailabilityChangedEvent;
		IronSourceEvents.onRewardedVideoAdStartedEvent -= RewardedVideoAdStartedEvent;
		IronSourceEvents.onRewardedVideoAdEndedEvent -= RewardedVideoAdEndedEvent;
		IronSourceEvents.onRewardedVideoAdRewardedEvent -= RewardedVideoAdRewardedEvent;
		IronSourceEvents.onRewardedVideoAdShowFailedEvent -= RewardedVideoAdShowFailedEvent;
		IronSourceEvents.onRewardedVideoAdClickedEvent -= RewardedVideoAdClickedEvent;
		
		IronSourceEvents.onInterstitialAdReadyEvent -= OnIntersAdReady;
		IronSourceEvents.onInterstitialAdClickedEvent -= OnIntersAdClick;
		IronSourceEvents.onInterstitialAdLoadFailedEvent -= OnIntersAdLoadFailed;
		IronSourceEvents.onInterstitialAdShowFailedEvent -= OnIntersAdShowFailed;
		IronSourceEvents.onInterstitialAdShowSucceededEvent -= OnIntersAdShowSuc;
		IronSourceEvents.onInterstitialAdOpenedEvent -= OnIntersAdOpend;
		IronSourceEvents.onInterstitialAdClosedEvent -= OnIntersAdClosed;
	}

	/// <summary>
	/// 设置玩家属性 必须在初始化App之前调用
	/// </summary>
	/// <param name="level">玩家等级</param>
	/// <param name="iapt">购买总金额</param>
	/// <param name="userCreationDate">创建时间 首次进游戏时间</param>
	/// <param name="att">自定义参数 最多5个 key和value都为string类型 </param>
	public void SetAdsUserInfo(int level, double iapt, long userCreationDate, LuaTable att)
	{
		string id = IronSource.Agent.getAdvertiserId();
		IronSource.Agent.setUserId(id);
		EzDebug.Log("unity-script: getAdvertiserId" + id);
	
		IronSourceSegment segment = new IronSourceSegment();
		segment.level = level;
		segment.iapt = iapt;
		segment.userCreationDate = userCreationDate;

		//自定义参数最多5个
		if (att != null)
		{
			att.ForEach<string, string>((k, v) =>
			{
				segment.customs.Add(k, v);
			});
		}

		IronSource.Agent.setSegment(segment);
	}


	void InitRewardAdsEvent() 
	{
		EzDebug.Log("unity-script: ShowRewardedVideoScript Start called");

		//Add Rewarded Video Events
		IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
		IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
		IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
		IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
		IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
		IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
		IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
		IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;
	}

	/// <summary>
	/// 插屏广告事件初始化
	/// </summary>
	private void InitIntersAdEvent()
	{
		IronSourceEvents.onInterstitialAdReadyEvent += OnIntersAdReady;
		IronSourceEvents.onInterstitialAdClickedEvent += OnIntersAdClick;
		IronSourceEvents.onInterstitialAdLoadFailedEvent += OnIntersAdLoadFailed;
		IronSourceEvents.onInterstitialAdShowFailedEvent += OnIntersAdShowFailed;
		IronSourceEvents.onInterstitialAdShowSucceededEvent += OnIntersAdShowSuc;
		IronSourceEvents.onInterstitialAdOpenedEvent += OnIntersAdOpend;
		IronSourceEvents.onInterstitialAdClosedEvent += OnIntersAdClosed;
	}
	#region 激励广告

	public void ShowRewardAds()
	{

#if UNITY_EDITOR
		Debug.Log("Editor play Reward ad once");
		if (_onRewardAds != null)
			_onRewardAds((int)Type.ADS_RESULT_FINISHT);
#else
		if (IronSource.Agent.isRewardedVideoAvailable())
		{
#if UNITY_IOS
			_timeScaleCache = Time.timeScale;
			Time.timeScale = 0;
			SoundManager.Instance.CacheAndMuteAll();
#endif
			IronSource.Agent.showRewardedVideo();
		}
		else
		{
			EzDebug.Log("unity-script: IronSource.Agent.isRewardedVideoAvailable - False");
			if (_onRewardAds != null)
				_onRewardAds((int)Type.ADS_RESULT_NOREADY);
		}
#endif

	}

	void RewardedVideoAvailabilityChangedEvent(bool canShowAd)
	{
		EzDebug.Log("unity-script: I got RewardedVideoAvailabilityChangedEvent, value = " + canShowAd);
		if (_onAvailability != null)
		{
			_onAvailability(canShowAd);
		}
	}

	void RewardedVideoAdOpenedEvent()
	{
		EzDebug.Log("unity-script: I got RewardedVideoAdOpenedEvent");
	}

	void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp)
	{
		EzDebug.Log("unity-script: I got RewardedVideoAdRewardedEvent, amount = " + ssp.getRewardAmount() + " name = " + ssp.getRewardName());
		if (ssp.getRewardAmount() > 0 && !string.IsNullOrEmpty(ssp.getRewardName()))
		{
			if (_onRewardAds != null)
				_onRewardAds((int)Type.ADS_RESULT_FINISHT);
		}
		else
		{
			if (_onRewardAds != null)
				_onRewardAds((int)Type.ADS_RESULT_SKIP);
		}
	}

	void RewardedVideoAdClosedEvent()
	{
#if UNITY_IOS
		Time.timeScale = _timeScaleCache;
		SoundManager.Instance.RevertMuteAll();
#endif
		EzDebug.Log("unity-script: I got RewardedVideoAdClosedEvent");
	}

	void RewardedVideoAdStartedEvent()
	{
		EzDebug.Log("unity-script: I got RewardedVideoAdStartedEvent");
	}

	void RewardedVideoAdEndedEvent()
	{
		EzDebug.Log("unity-script: I got RewardedVideoAdEndedEvent");
	}

	void RewardedVideoAdShowFailedEvent(IronSourceError error)
	{
#if UNITY_IOS
		Time.timeScale = _timeScaleCache;
		SoundManager.Instance.RevertMuteAll();
#endif
		EzDebug.Log("unity-script: I got RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
		if (_onRewardAds != null)
			_onRewardAds((int)Type.ADS_RESULT_FAILED);
	}

	void RewardedVideoAdClickedEvent(IronSourcePlacement ssp)
	{
		EzDebug.Log("unity-script: I got RewardedVideoAdClickedEvent, name = " + ssp.getRewardName());
	}
	#endregion
	
	#region 插屏广告

	/// <summary>
	/// 加载插屏广告
	/// 加载完毕后检查是否ready，这里不用做检查
	/// </summary>
	public void LoadIntersAds()
	{
		IronSource.Agent.loadInterstitial();
	}

        
	public void ShowIntersAds()
	{
#if UNITY_EDITOR
		Debug.Log("Editor play Interstitial ad once");
		_onIntersAd?.Invoke((int)Type.ADS_RESULT_FINISHT);
#else
       	bool intersAdReady = IronSource.Agent.isInterstitialReady();
       	Debug.Log($"[Inters Ads]Inters Ad is ready! Result:{intersAdReady}");
       	if (intersAdReady)
       	{
       	    IronSource.Agent.showInterstitial(); 
       	}
       	else
       	{
       	    IronSource.Agent.loadInterstitial();
       	    Debug.Log($"[Inters Ads]Inters Ad is not ready!");
       	    _onIntersAd?.Invoke((int)Type.ADS_RESULT_NOREADY);
       	}
#endif
	}
	
	/// <summary>
	/// 广告加载完毕回调
	/// 加载成功显示广告，设置
	/// </summary>
	private void OnIntersAdReady()
	{
		Debug.Log($"[Inters Ads] {IronSource.Agent.isInterstitialReady()}");
	}
        
	private void OnIntersAdClick()
	{
		
	}
        
	private void OnIntersAdLoadFailed(IronSourceError err)
	{
	}
        
	private void OnIntersAdShowFailed(IronSourceError err)
	{
		_onIntersAd?.Invoke((int)Type.ADS_RESULT_FAILED);
		Debug.Log($"[Inters Ads]Inters Ad show failed{err.ToString()}");
	}
        
	private void OnIntersAdShowSuc()
	{
	}
        
	private void OnIntersAdOpend()
	{
	}
        
	private void OnIntersAdClosed()
	{
		_onIntersAd?.Invoke((int)Type.ADS_RESULT_FINISHT);
		Debug.Log($"[Inters Ads]Inters Ad show CLOSED");
		IronSource.Agent.loadInterstitial();
	}
	#endregion
}
