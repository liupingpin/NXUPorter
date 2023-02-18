//
//  NdUnityAppController.m
//  Unity-iPhone
//
//  Created by nd on 2019/5/20.
//

#import "UnityAppController.h"

#import "SdkMgr.h"
#include <UIKIT/UIAlertView.h>
#include <string>
#include "NdReachability.h"
#import <CoreTelephony/CTTelephonyNetworkInfo.h>
#import <CoreTelephony/CTCarrier.h>
#import <AdSupport/AdSupport.h>

extern void UnitySendMessage (const char* obj, const char* method, const char* msg);
//Unity string传递过来的字符串正确转换成NSString
static NSString* CreateNSString (const char* string) {
    return [NSString stringWithUTF8String:(string ? string : "")];
}
//NSString传递给Unity的字符串正确转换成string
static const char* NSString2Char(NSString* sl){
    char* ret = nullptr;
    ret = (char*) malloc([sl length] + 1);
    memcpy(ret,[sl UTF8String],([sl length] + 1));
    return ret;
}
//Dictionary转成JSON
static const char* EnJson(NSMutableDictionary *dictionary){
    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dictionary options:NSJSONWritingPrettyPrinted error:&error];
    if (error)
    {
        NSLog(@"dic->%@",error);
        return NULL;
    }
    else
    {
        NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        return NSString2Char(jsonString);
    }
}
//JSON转成Dictionary
static NSMutableDictionary* DenJson(const char* json){
    NSString* jsonString = CreateNSString(json);
    if (jsonString == nil) {
        return nil;
    }
    
    NSData *jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *err;
    NSMutableDictionary *dic = [NSJSONSerialization JSONObjectWithData:jsonData
                                                        options:NSJSONReadingMutableContainers
                                                          error:&err];
    if(err) {
        NSLog(@"json解析失败：%@",err);
        return nil;
    }
    return dic;
}
/*static NSNumber* int2NSNumber(int num){
    return [NSNumber numberWithInt:num];
}
static NSString* std_string2NSString(std::string str){
   return [NSString stringWithCString:str.c_str() encoding:[NSString defaultCStringEncoding]];
}*/

@interface NdUnityAppController : UnityAppController
@property (nonatomic) NdReachability *hostReachability;
@property (nonatomic) NdReachability *internetReachability;
    +(NSString*)getIPhoneNotchScreenHeight;
    +(void)requestNotification;
@end

IMPL_APP_CONTROLLER_SUBCLASS(NdUnityAppController)

class NdSdkCallback : public INdSdkCallback
{
public:
    // 初始化
    virtual void OnInitResult(NSMutableDictionary* dictData)
    {
        NSLog(@" OnInitResult pause = %d",UnityIsPaused());
        //请求推送权限
        [NdUnityAppController requestNotification];
        UnitySendMessage("NdLauncher","StartLauncher","");
        UnitySendMessage("SdkManager", "OnInitResult", EnJson(dictData));
    }
    
    // 登陆
    virtual void OnLogin(NSMutableDictionary* dictData)
    {
        UnitySendMessage("SdkManager", "OnLogin", EnJson(dictData));
    }
    
    // 充值
    virtual void OnRecharge(NSMutableDictionary* dictData)
    {
        UnitySendMessage("SdkManager", "OnRecharge", EnJson(dictData));
    }
    
    // 登出
    virtual void OnLogout(NSMutableDictionary* dictData)
    {
        UnitySendMessage("SdkManager", "OnLogout", EnJson(dictData));
    }
    
    virtual void OnSpecialOperate(NSMutableDictionary* dictData)
    {
        UnitySendMessage("SdkManager", "OnSpecialOperate", EnJson(dictData));
    }
};
NdSdkCallback g_ndSdkCallback;

@implementation NdUnityAppController
{
    NSString *urlStr;
}

-(BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    [super application:application didFinishLaunchingWithOptions:launchOptions];
    [SdkMgr setNdCallback:&g_ndSdkCallback];

    //初始化SDK
    [SdkMgr application:application didFinishLaunchingWithOptions:launchOptions];
    //添加网络监听
    [self listenNetWorkingStatus];

    return YES;
}
/*- (BOOL)application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation{
    //Unity 的OpenUrl 有问题的话 关闭
    [super application:application openURL:url sourceApplication:sourceApplication annotation:annotation];
    
    return [SdkMgr application:application openURL:url sourceApplication:sourceApplication annotation:annotation];
}*/
-(BOOL)application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey,id> *)options{
    if (url) {
        urlStr = url.absoluteString;
    }
    return [SdkMgr application:app openURL:url options:options];
}
-(void)application:(UIApplication *)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken{
    [super application:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];
    [SdkMgr application:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];
}
-(BOOL)application:(UIApplication *)application handleOpenURL:(NSURL *)url{
    return [SdkMgr application:application handleOpenURL:url];
}
-(void)applicationWillTerminate:(UIApplication *)application{
    [SdkMgr applicationWillTerminate:application];
    [super applicationWillTerminate:application];
}
-(void)applicationDidBecomeActive:(UIApplication *)application{
    [SdkMgr applicationDidBecomeActive:application];
    [super applicationDidBecomeActive:application];
}
-(void)applicationWillEnterForeground:(UIApplication *)application{
    [SdkMgr applicationWillEnterForeground:application];
    [super applicationWillEnterForeground:application];
}
-(void)applicationDidEnterBackground:(UIApplication *)application{
    [SdkMgr applicationDidEnterBackground:application];
    [super applicationDidEnterBackground:application];
}

+(void) requestNotification
{
    UIUserNotificationSettings *setting = [UIUserNotificationSettings settingsForTypes:UIUserNotificationTypeAlert | UIUserNotificationTypeBadge | UIUserNotificationTypeSound categories:nil];
    
    //注册，会触发权限请求
    [[UIApplication sharedApplication] registerUserNotificationSettings:setting];
}

//获取刘海屏 刘海的高度
+(NSString*)getIPhoneNotchScreenHeight
{
    //iOS 11 以下的默认不存在刘海儿（没有谁iphone X 刷回ios 10 系统吧。哈哈）
    if (__IPHONE_OS_VERSION_MAX_ALLOWED < __IPHONE_11_0) {
        return @"0";
    }
    
    /* iPhone8 Plus  UIEdgeInsets: {20, 0, 0, 0}
     * iPhone8       UIEdgeInsets: {20, 0, 0, 0}
     * iPhone XR     UIEdgeInsets: {44, 0, 34, 0}
     * iPhone XS     UIEdgeInsets: {44, 0, 34, 0}
     * iPhone XS Max UIEdgeInsets: {44, 0, 34, 0}
     */
    //ios 11 以上提供专门方法获取刘海儿高度
    CGFloat bottomSpace = 0;
    if (@available(iOS 11.0, *)) {
        UIEdgeInsets safeAreaInsets = UIApplication.sharedApplication.windows.firstObject.safeAreaInsets;
        
        switch (UIApplication.sharedApplication.statusBarOrientation) {
            case UIInterfaceOrientationPortrait:{
                bottomSpace = safeAreaInsets.bottom;
            }break;
            case UIInterfaceOrientationLandscapeLeft:{
                bottomSpace = safeAreaInsets.right;
            }break;
            case UIInterfaceOrientationLandscapeRight:{
                bottomSpace = safeAreaInsets.left;
            } break;
            case UIInterfaceOrientationPortraitUpsideDown:{
                bottomSpace = safeAreaInsets.top;
            }break;
            default:
                bottomSpace = safeAreaInsets.bottom;
                break;
        }
    }
    return [NSString stringWithFormat:@"%f",bottomSpace];
}
#pragma mark - 判断是否是iPhoneX
static inline BOOL isIPhoneXSeries() {
    BOOL iPhoneXSeries = NO;
    if (UIDevice.currentDevice.userInterfaceIdiom != UIUserInterfaceIdiomPhone) {
        return iPhoneXSeries;
    }
    
    if (@available(iOS 11.0, *)) {
        UIWindow *mainWindow = [[[UIApplication sharedApplication] delegate] window];
        if (mainWindow.safeAreaInsets.bottom > 0.0) {
            iPhoneXSeries = YES;
        }
    }
    return iPhoneXSeries;
}

#pragma mark -添加网络监听
-(void)listenNetWorkingStatus{
    //NSLog(@"listenNetWorkingStatus");
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(reachabilityChanged:) name:kNdReachabilityChangedNotification object:nil];
    self.internetReachability = [NdReachability NdreachabilityForInternetConnection];
    [self.internetReachability startNotifier];
    [self updateInterfaceWithReachability:self.internetReachability];
}
- (void) reachabilityChanged:(NSNotification *)note
{
    NdReachability* curReach = [note object];
    [self updateInterfaceWithReachability:curReach];
}
- (void)updateInterfaceWithReachability:(NdReachability *)reachability
{    
    NetworkStatus netStatus = [reachability currentReachabilityStatus];
    switch (netStatus) {
        case 0:
            //NSLog(@"NotReachable----无网络");
            UnitySendMessage("SdkManager", "OnNettypeChanged", "-1");
            UnitySendMessage("SdkManager", "OnSignalStrengthChanged", "0");
            break;
        case 1:
            //NSLog(@"ReachableViaWiFi----WIFI");
            UnitySendMessage("SdkManager", "OnNettypeChanged", "1");
            UnitySendMessage("SdkManager", "OnSignalStrengthChanged", NSString2Char([self getSignalStrength]));
            break;
        case 2:
            //NSLog(@"ReachableViaWWAN----蜂窝网络");
            UnitySendMessage("SdkManager", "OnNettypeChanged", NSString2Char([self getNetworkType]));
            UnitySendMessage("SdkManager", "OnSignalStrengthChanged", NSString2Char([self getSignalStrength]));
            break;
        default:
            //NSLog(@"ReachableViaWWAN----default网络");
            UnitySendMessage("SdkManager", "OnNettypeChanged", NSString2Char([self getNetworkType]));
            UnitySendMessage("SdkManager", "OnSignalStrengthChanged", NSString2Char([self getSignalStrength]));
            break;
    }
    
}
- (void)dealloc
{
    [[NSNotificationCenter defaultCenter] removeObserver:self name:kNdReachabilityChangedNotification object:nil];
}
- (NSString *)getIntentExtras{
    if (urlStr) {
        return urlStr;
    }
    return @"";
}

#pragma mark - 获取使用蜂窝网络时候的具体网络类型
- (NSString *)getNetworkType{
    CTTelephonyNetworkInfo *info = [CTTelephonyNetworkInfo new];
    NSString *networkType = @"";
    if ([info respondsToSelector:@selector(currentRadioAccessTechnology)]) {
        NSString *currentStatus = info.currentRadioAccessTechnology;
        NSArray *network2G = @[CTRadioAccessTechnologyGPRS, CTRadioAccessTechnologyEdge, CTRadioAccessTechnologyCDMA1x];
        NSArray *network3G = @[CTRadioAccessTechnologyWCDMA, CTRadioAccessTechnologyHSDPA, CTRadioAccessTechnologyHSUPA, CTRadioAccessTechnologyCDMAEVDORev0, CTRadioAccessTechnologyCDMAEVDORevA, CTRadioAccessTechnologyCDMAEVDORevB, CTRadioAccessTechnologyeHRPD];
        NSArray *network4G = @[CTRadioAccessTechnologyLTE];
        
        if ([network2G containsObject:currentStatus]) {
            networkType = @"2G";
        }else if ([network3G containsObject:currentStatus]) {
            networkType = @"3G";
        }else if ([network4G containsObject:currentStatus]){
            networkType = @"4G";
        }else {
            networkType = @"未知网络";
        }
    }
    return networkType;
}
#pragma mark - 获取网络信号强度
-(NSString *)getSignalStrength {
    NSString *signalStrength = @"3";
    return signalStrength;
    /*id statusBar;
    NSString *signalStrength = @"";
    if (@available(iOS 13.0, *)) {*/
        /*UIStatusBarManager *statusBarManager = [UIApplication sharedApplication].keyWindow.windowScene.statusBarManager;
        if ([statusBarManager respondsToSelector:@selector(createLocalStatusBar)]) {
            UIView *localStatusBar = [statusBarManager performSelector:@selector(createLocalStatusBar)];
            if ([localStatusBar respondsToSelector:@selector(statusBar)]) {
                statusBar = [localStatusBar performSelector:@selector(statusBar)];
            }
        }
        
        if (statusBar) {
            id currentData = [[statusBar valueForKeyPath:@"_statusBar"] valueForKeyPath:@"currentData"];
            id wifiEntry = [currentData valueForKeyPath:@"wifiEntry"];
            id cellularEntry = [currentData valueForKeyPath:@"cellularEntry"];
            if (wifiEntry && [[wifiEntry valueForKeyPath:@"isEnabled"] boolValue]) {
                signalStrength = [[wifiEntry valueForKeyPath:@"displayValue"] intValue];
                signalStrength = signalStrength == 3 ? 4 : signalStrength;
            } else if (cellularEntry && [[cellularEntry valueForKeyPath:@"isEnabled"] boolValue]) {
                signalStrength = [[cellularEntry valueForKey:@"displayValue"] intValue];
            }
        }
        return signalStrength;*/
        /*return @"not support ios 13!!";
    }
    UIApplication *app = [UIApplication sharedApplication];
    statusBar = [app valueForKey:@"statusBar"];
    if (isIPhoneXSeries()) {
        //iPhone X
        id statusBarView = [statusBar valueForKeyPath:@"statusBar"];
        UIView *foregroundView = [statusBarView valueForKeyPath:@"foregroundView"];
        NSArray *subviews = [[foregroundView subviews][2] subviews];
        // 非WIFI
        if (![[self getNetworkType] isEqualToString:@"WIFI"] && ![[self getNetworkType] isEqualToString:@"NONE"]) {
            for (id subview in subviews) {
                if ([subview isKindOfClass:NSClassFromString(@"_UIStatusBarCellularSignalView")]) {
                    signalStrength = [NSString stringWithFormat:@"%@",[subview valueForKey:@"_numberOfActiveBars"]];
                }
            }
        }
        // WIFI
        if ([[self getNetworkType] isEqualToString:@"WIFI"] && ![[self getNetworkType] isEqualToString:@"NONE"]) {
            for (id subview in subviews) {
                if ([subview isKindOfClass:NSClassFromString(@"_UIStatusBarWifiSignalView")]) {
                    signalStrength = [NSString stringWithFormat:@"%@",[subview valueForKey:@"_numberOfActiveBars"]];
                }
            }
        }
    } else {
        NSArray *subviews = [[[app valueForKey:@"statusBar"] valueForKey:@"foregroundView"] subviews];
        NSString *dataNetworkItemView = nil;
        if ([[self getNetworkType] isEqualToString:@"WIFI"] && ![[self getNetworkType] isEqualToString:@"NONE"]) {
            //WiFi
            for (id subview in subviews) {
                if([subview isKindOfClass:[NSClassFromString(@"UIStatusBarDataNetworkItemView") class]]) {
                    dataNetworkItemView = subview;
                    signalStrength = [NSString stringWithFormat:@"%@",[dataNetworkItemView valueForKey:@"_wifiStrengthBars"]];
                    break;
                }
            }
        }
        
        if (![[self getNetworkType] isEqualToString:@"WIFI"] && ![[self getNetworkType] isEqualToString:@"NONE"]) {
            //非WIFI
            for (id subview in subviews) {
                if ([subview isKindOfClass:[NSClassFromString(@"UIStatusBarSignalStrengthItemView") class]]) {
                    dataNetworkItemView = subview;
                    signalStrength = [NSString stringWithFormat:@"%@",[dataNetworkItemView valueForKey:@"_signalStrengthBars"]];
                    break;
                }
            }
        }
    }
    return signalStrength;*/
}
#pragma mark -运营商
+ (NSString *)getCarrierInfo{
    CTTelephonyNetworkInfo *telephonyInfo = [[CTTelephonyNetworkInfo alloc] init];
    CTCarrier *carrier = [telephonyInfo subscriberCellularProvider];
    if(!carrier.isoCountryCode){
        return @"无SIM卡";
    }
    NSString *carrierName=[carrier carrierName];
    return carrierName;
};
#pragma mark -无法获取IMEI IMSI 用广告标识符代替
+ (NSString *)getAdvertisingIdentifier {
    return [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
}
#pragma mark -获取iccid 也是假的。
+ (NSString *)getIccid{
    CTTelephonyNetworkInfo *telephonyInfo = [[CTTelephonyNetworkInfo alloc] init];
    CTCarrier *carrier = [telephonyInfo subscriberCellularProvider];
    NSString *carrierCode = [carrier mobileNetworkCode];
    return [NSString stringWithFormat:@"%@", carrierCode];
}
@end
inline NdUnityAppController* GetNdAppController()
{
    return (NdUnityAppController*)[UIApplication sharedApplication].delegate;
}
extern "C" {
    
    const char * getAppId(){
        return NSString2Char([NSString stringWithFormat:@"%ld", (long)[SdkMgr getSdkAppId]]);
    }
    
    int getSdkType(){
        return (int)[SdkMgr getSdk_type];
    }
    
    const char*  getChannelId(){
        return NSString2Char([NSString stringWithFormat:@"%ld", (long)[SdkMgr getPlatform_id]]);
    }
	const char* getSubChannelId(){
		return NSString2Char([SdkMgr getSubChannelId]);
	}
    
    void sdkLogin(){
        [SdkMgr Login];
    }
    
    void sdkLogout(){
        [SdkMgr Logout];
    }
    
    const char* getNotchScreenHeight()
    {
        return NSString2Char([NdUnityAppController getIPhoneNotchScreenHeight]);
    }
    //上报角色数据
    void uploadData(const char* msgid,const char* strJson){
        NSMutableDictionary* dict = DenJson(strJson);
        if(dict!=nil){
            [SdkMgr uploadData:CreateNSString(msgid) data:dict];
        }
    }
    const char* getImei(){
        return NSString2Char([NdUnityAppController getAdvertisingIdentifier]);
    }
    const char* getIccid(){
        return NSString2Char([NdUnityAppController getIccid]);
    }
    const char* getImsi(){
         return NSString2Char([NdUnityAppController getAdvertisingIdentifier]);
    }
    const char* getCarrier(){
         return NSString2Char([NdUnityAppController getCarrierInfo]);
    }
    const char* getIntentExtras(){
        return NSString2Char([GetNdAppController() getIntentExtras]);
    }
    
    //充值
    void recharge(const char* strJson){
        NSMutableDictionary* dict = DenJson(strJson);
        if(dict!=nil){
            [SdkMgr recharge:dict];
        }
    }
    //初始化sdk
    void initSdk(){
        NSLog(@" initSdk pause = %d",UnityIsPaused());
        [SdkMgr InitSdk];
    }
	//弹出隐私协议
    bool openPrivacyAgreement(){
        return [SdkMgr openPrivacyAgreement];
    }
    //弹出用户协议
    bool openLicenceAgreement(){
        return [SdkMgr openLicenceAgreement];
    }
    //打开论坛
    bool openForum(){
        return [SdkMgr openForum];
    }
    //注销
    bool logOff(const char* roleName,const char* serverName,int level,const char* createTime){
        return [SdkMgr logOff:CreateNSString(roleName) serverName:CreateNSString(serverName) level:[NSNumber numberWithInt:level] createTime:CreateNSString(createTime)];
    }
    #pragma mark bi数据
    void trackPlayerLogin(const char* msg){
        [SdkMgr trackPlayerLogin:msg];
    }
    void trackPlayerLevelChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerLevelChangeUpload:msg1 data:msg2];
    }
    void trackPlayerGoldChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerGoldChangeUpload:msg1 data:msg2];
    }
    void trackPlayerItemChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerItemChangeUpload:msg1 data:msg2];
    }
    void trackPlayerGuideFlowUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerGuideFlowUpload:msg1 data:msg2];
    }
    void trackPlayerStageFlowUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerStageFlowUpload:msg1 data:msg2];
    }
    void trackPlayerMissionFlowUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerMissionFlowUpload:msg1 data:msg2];
    }
    void trackPlayerActivityActUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerActivityActUpload:msg1 data:msg2];
    }
    void trackPlayerGuildChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerGuildChangeUpload:msg1 data:msg2];
    }
    void trackPlayerCardChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerCardChangeUpload:msg1 data:msg2];
    }
    void trackPlayerCardOpChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerCardOpChangeUpload:msg1 data:msg2];
    }
    void trackPlayerEquipOpChangeUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerEquipOpChangeUpload:msg1 data:msg2];
    }
    void trackPlayerGachaUpload(const char*  msg1, const char*  msg2){
        [SdkMgr trackPlayerGachaUpload:msg1 data:msg2];
    }
    
}
