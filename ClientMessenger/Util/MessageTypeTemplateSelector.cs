using System.Windows;
using System.Windows.Controls;
using ClientMessenger.Models;

namespace ClientMessenger.Util
{
    public class MessageTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextMessageTemplate { get; set; }
        public DataTemplate ImageMessageTemplate { get; set; }
        public DataTemplate FileMessageTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MessageModel message)
            {
                switch (message.Type.ToLower())
                {
                    case "text":
                        return TextMessageTemplate;
                    case "image":
                        return ImageMessageTemplate;
                    case "file":
                        return FileMessageTemplate;
                    default:
                        return TextMessageTemplate; 
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
