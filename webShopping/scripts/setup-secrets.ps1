# إعداد الأسرار محلياً (Development)
# شغّل من مجلد webShopping بعد تعديل القيم:

cd webShopping

# Iyzipay (Sandbox للتجربة / Production للإطلاق)
dotnet user-secrets set "Iyzipay:ApiKey" "YOUR_IYZIPAY_API_KEY"
dotnet user-secrets set "Iyzipay:SecretKey" "YOUR_IYZIPAY_SECRET_KEY"
dotnet user-secrets set "Iyzipay:BaseUrl" "https://sandbox-api.iyzipay.com"

# PayPal (اختياري)
dotnet user-secrets set "PayPal:ClientId" "YOUR_PAYPAL_CLIENT_ID"
dotnet user-secrets set "PayPal:Secret" "YOUR_PAYPAL_SECRET"
dotnet user-secrets set "PayPal:Mode" "sandbox"

# SMTP — لإرسال إيميلات تأكيد الطلب
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:User" "your@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-app-password"
dotnet user-secrets set "Smtp:From" "noreply@sagershop.com"
dotnet user-secrets set "Smtp:FromName" "SagerShop"

# Twilio SMS (اختياري)
dotnet user-secrets set "Sms:Enabled" "true"
dotnet user-secrets set "Sms:Provider" "Twilio"
dotnet user-secrets set "Sms:TwilioAccountSid" "YOUR_SID"
dotnet user-secrets set "Sms:TwilioAuthToken" "YOUR_TOKEN"
dotnet user-secrets set "Sms:TwilioFromNumber" "+1234567890"

# كلمة مرور الأدمن (مطلوبة — لا تستخدم Admin@123)
dotnet user-secrets set "AdminSeed:Password" "YourStrongPassword@2026!"
# لتحديث كلمة مرور الأدمن الحالية مرة واحدة:
dotnet user-secrets set "AdminSeed:ResetPassword" "true"
# بعد أول تشغيل ناجح، أزل ResetPassword أو اضبطه false

# OAuth (اختياري)
dotnet user-secrets set "Authentication:Facebook:AppId" "YOUR_FB_APP_ID"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "YOUR_FB_SECRET"
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_SECRET"

# للإنتاج: استخدم متغيرات البيئة بدلاً من User Secrets
# مثال: Iyzipay__ApiKey, Smtp__Host, AdminSeed__Password
