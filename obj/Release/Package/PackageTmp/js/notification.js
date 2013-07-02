var notification = {
  "collection": "timeline",
  "itemId": "3hidvm0xez6r8_dacdb3103b8b604_h8rpllg",
  "operation": "UPDATE",
  "userToken": "harold_penguin",
  "verifyToken": "random_hash_to_verify_referer",
  "userActions": [
    {
      "type": "SHARE"
    }
  ],
  "id": "3hidvm0xez6r8_dacdb3103b8b604_h8rpllg",
  "attachments": [
      {
          "contentType": "image/jpeg",
          "id": "<ATTACHMENT_ID>",
          "contentUrl": "https://www.googleapis.com/mirror/v1/attachments/3hidvm0xez6r8_dacdb3103b8b604_h8rpllg/<ATTACHMENT_ID>"
      }
  ],
  "recipients": [
      {
          "kind": "glass#contact",
          "source": "api:<SERVICE_ID>",
          "id": "<CONTACT_ID>",
          "displayName": "<CONTACT_DISPLAY_NAME>",
          "imageUrls": [
              "<CONTACT_ICON_URL>"
          ]
      }
  ]
}

$.ajax({
url: "https://myglassapps.com/notification.ashx",
type: "POST",
dataType: "json",
data: JSON.stringify(notification),
success:function(request){
console.log(request);
}
});
