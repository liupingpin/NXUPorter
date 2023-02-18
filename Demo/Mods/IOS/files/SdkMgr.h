//
//  SdkMgr.h
//  Unity-iPhone
//  插件接入
//  Created by nd on 2019/5/20.
//

#ifndef SdkMgr_h
#define SdkMgr_h

#import <Foundation/Foundation.h>

class INdSdkCallback
{
public:
    // 初始化
    virtual void OnInitResult(NSMutableDictionary* dictData) = 0 ;
    // 登陆
    virtual void OnLogin(NSMutableDictionary* dictData) = 0;
        // 充值
    virtual void OnRecharge(NSMutableDictionary* dictData) = 0;
    // 登出
    virtual void OnLogout(NSMutableDictionary* dictData) = 0;
    // 特殊操作
    virtual void OnSpecialOperate(NSMutableDictionary* dictData) = 0;
};

@interface SdkMgr : NSObject

+(void)setNdCallback:(INdSdkCallback*) callback;

//*********************************/
//SdkMgr 包含所有sdk的接口，除开通用的接口在这里实现，其他的特殊接口分别在不同的sdk中实现。没用用到的就不做任何实现就行。
//*********************************/
//登入
+(void)Login;
//登出
+(void)Logout;
//充值
+(void)recharge:(NSMutableDictionary *)dictData;
//用户信息绑定
+(void)userBindInfo;
//用户信息绑定界面关闭
+(void)closeUserBindInfo;
//上传数据
+(void)uploadData:(NSString*)msgId data:(NSMutableDictionary *)dictData;
//实名认证查询
+(void)queryRealNameState;
//实名认证绑定
+(void)openRealNameAuth;
//获取登录账号类型
+ (void)queryLoginState;
//初始化Sdk
+(void)InitSdk;

//为了分离SDKDefines出 无sdk的版本在这里做个桥接
//获取sdkAppId
+(NSInteger) getSdkAppId;
//获取sdk_type
+(NSInteger) getSdk_type;
//获取platform_id
+(NSInteger) getPlatform_id;
//获取子渠道id
+(NSString *)getSubChannelId;
//第三方用户信息绑定
+(void)bindThirdUserInfo:(NSString*) isOuterNet;
//三方用户绑定查询
+(void)searchThirdUserInfo:(NSString*) isOuterNet;
+(bool)openPrivacyAgreement;
+(bool)openLicenceAgreement;
+(bool)openForum;
+(bool)logOff:(NSString*) roleName serverName:(NSString*)serverName level:(NSNumber*)level createTime:(NSString*)createTime;

#pragma mark 重写系统方法
+(BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions;

+(BOOL)application:(UIApplication *)application openURL:(NSURL *)url sourceApplication:(NSString *)sourceApplication annotation:(id)annotation;

+(BOOL)application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey,id> *)options;

+(NSUInteger)application:(UIApplication *)application supportedInterfaceOrientationsForWindow:(UIWindow *)window;

+(void)application:(UIApplication *)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken;

+(BOOL)application:(UIApplication *)application handleOpenURL:(NSURL *)url;

+(void)applicationWillTerminate:(UIApplication *)application;

+(void)applicationDidBecomeActive:(UIApplication *)application;

+(void)applicationWillEnterForeground:(UIApplication *)application;

+(void)applicationDidEnterBackground:(UIApplication *)application;

#pragma mark bi数据
+(void)trackPlayerLogin:(const char*)msg;
+(void)trackPlayerLevelChangeUpload:(const char*) msg1 data:(const char*) msg2;
+(void)trackPlayerGoldChangeUpload:(const char*) msg1 data:(const char*) msg2;
+(void)trackPlayerItemChangeUpload:(const char*) msg1 data:(const char*) msg2;
+(void)trackPlayerGuideFlowUpload:(const char*) msg1 data:(const char*) msg2;
+(void)trackPlayerStageFlowUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerMissionFlowUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerActivityActUpload:(const char*) msg1 data:(const char*) msg2;
+(void)trackPlayerGuildChangeUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerCardChangeUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerCardOpChangeUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerEquipOpChangeUpload:(const char*)  msg1 data:(const char*) msg2;
+(void)trackPlayerGachaUpload:(const char*)  msg1 data:(const char*) msg2;

@end

#endif /* SdkMgr_h */
