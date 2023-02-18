//
//  SdkMgr.m
//  Unity-iPhone
//
//  Created by nd on 2019/5/20.
//

#import <Foundation/Foundation.h>
#include <string>
#import "SdkMgr.h"

static NSNumber* int2NSNumber(int num){
    return [NSNumber numberWithInt:num];
}
static NSString* std_string2NSString(std::string str){
    return [NSString stringWithCString:str.c_str() encoding:[NSString defaultCStringEncoding]];
}

static INdSdkCallback* ndSdkCallback;

///////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
@implementation SdkMgr
//为了分离SDKDefines出 无sdk的版本在这里做个桥接
//获取sdkAppId
+(NSInteger) getSdkAppId{
    return 1;
}
//获取sdk_type
+(NSInteger) getSdk_type{
    return 0;
}
//获取platform_id
+(NSInteger) getPlatform_id{
    return 1;
}
//获取子渠道id
+(NSString *) getSubChannelId{
	return @"";
}

+(void)setNdCallback:(INdSdkCallback*) callback{
    ndSdkCallback = callback;
}

+(void)InitSdk{
    //初始化会调
	NSMutableDictionary *dictionary = [[NSMutableDictionary alloc] init];
    [dictionary setValue:int2NSNumber(1) forKey:@"code"];
    [dictionary setValue:@"" forKey:@"msg"];
    ndSdkCallback->OnInitResult(dictionary);
}
+(void)setQuickRegister {

}
+(void)binderPhoneGuide {
}
+(void)setShowLoginBackground{
}
+(void)setShowThirdLogin {
}
+ (void)setNDLogType {

}
+(void)Login{

}

+(void)Logout{
}

+(void)recharge:(NSMutableDictionary *)dictData; {

}
//用户信息绑定
+(void)userBindInfo {
}
//用户信息绑定界面关闭
+(void)closeUserBindInfo {

}
//上报数据
+(void)uploadData:(NSString*)msgId data:(NSMutableDictionary *)dictData{
    
}
+(void)queryRealNameState {

}
+(void)openRealNameAuth{

}
+(void)queryLoginState{

}

//第三方用户信息绑定
+(void)bindThirdUserInfo:(NSString*) isOuterNet{

}
//三方用户绑定查询
+(void)searchThirdUserInfo:(NSString*) isOuterNet{

}
+(bool)openPrivacyAgreement{
    return false;
}
+(bool)openLicenceAgreement{
    return false;
}
+(bool)openForum{
    return false;
}
+(bool)logOff:(NSString*) roleName serverName:(NSString*)serverName level:(NSNumber*)level createTime:(NSString*)createTime{
    return false;
}

//系统重写部分
+(BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions{
    return YES;
}

+(BOOL)application:(UIApplication *)application openURL:(NSURL *)url sourceApplication:(NSString *)sourceApplication annotation:(id)annotation{
    return YES;
}

+(BOOL)application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey,id> *)options{
    return YES;
}
//TODO:暂时不管
+(NSUInteger)application:(UIApplication *)application supportedInterfaceOrientationsForWindow:(UIWindow *)window{
    return 0;
}

+(void)application:(UIApplication *)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken{

}

+(BOOL)application:(UIApplication *)application handleOpenURL:(NSURL *)url{
    return YES;
}

+(void)applicationWillTerminate:(UIApplication *)application{
    
}

+(void)applicationDidBecomeActive:(UIApplication *)application{
    
}

+(void)applicationWillEnterForeground:(UIApplication *)application{
    
}

+(void)applicationDidEnterBackground:(UIApplication *)application{
}
#pragma mark bi数据
+(void)trackPlayerLogin:(const char*)msg{
}
+(void)trackPlayerLevelChangeUpload:(const char*) msg1 data:(const char*) msg2{
}
+(void)trackPlayerGoldChangeUpload:(const char*) msg1 data:(const char*) msg2{
}
+(void)trackPlayerItemChangeUpload:(const char*) msg1 data:(const char*) msg2{
}
+(void)trackPlayerGuideFlowUpload:(const char*) msg1 data:(const char*) msg2{
}
+(void)trackPlayerStageFlowUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerMissionFlowUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerActivityActUpload:(const char*) msg1 data:(const char*) msg2{
}
+(void)trackPlayerGuildChangeUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerCardChangeUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerCardOpChangeUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerEquipOpChangeUpload:(const char*)  msg1 data:(const char*) msg2{
}
+(void)trackPlayerGachaUpload:(const char*)  msg1 data:(const char*) msg2{
}


@end
